using Market.Domain;
using Market.Domain.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Application.Services;

/// <summary>
/// Regras de negócio de mercadorias, independentes da UI: validação, conversão de
/// preços para centavos, código de barras único, listagem filtrada, edição e
/// exclusão lógica.
/// </summary>
public class MercadoriaService
{
    private const int SqliteConstraint = 19; // SQLITE_CONSTRAINT

    private readonly IMercadoriaRepository _repositorio;
    private readonly ILogger<MercadoriaService> _logger;

    public MercadoriaService(IMercadoriaRepository repositorio, ILogger<MercadoriaService> logger)
    {
        _repositorio = repositorio;
        _logger = logger;
    }

    public Task<IReadOnlyList<Mercadoria>> ListarAsync(
        FiltroMercadoria filtro, CancellationToken cancellationToken = default)
        => _repositorio.ListarAsync(filtro, cancellationToken);

    public async Task<ResultadoOperacao> CadastrarAsync(
        CadastroMercadoriaDados dados, CancellationToken cancellationToken = default)
    {
        var erros = ValidarCampos(dados, out var nome, out var codigoBarras);
        if (erros.Count > 0)
            return ResultadoOperacao.Falha(erros);

        // Código de barras já usado: se for de uma mercadoria INATIVA, reativa (produto voltou);
        // se for de uma ATIVA, é duplicado de verdade.
        if (codigoBarras is not null)
        {
            var existente = await _repositorio
                .ObterPorCodigoBarrasIncluindoInativaAsync(codigoBarras, cancellationToken);
            if (existente is not null)
            {
                if (existente.Ativo)
                    return ResultadoOperacao.Falha(
                        $"Já existe uma mercadoria com o código de barras {codigoBarras}.");

                existente.Ativo = true;
                AplicarDados(existente, dados, nome, codigoBarras);
                await _repositorio.UpdateAsync(existente, cancellationToken);
                _logger.LogInformation("Mercadoria reativada pelo código {Codigo} (Id {Id}).",
                    codigoBarras, existente.Id);
                return ResultadoOperacao.Ok(existente.Id);
            }
        }

        var mercadoria = new Mercadoria { Ativo = true };
        AplicarDados(mercadoria, dados, nome, codigoBarras);

        try
        {
            await _repositorio.AddAsync(mercadoria, cancellationToken);
        }
        catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
        {
            _logger.LogWarning(ex, "Violação de unicidade ao cadastrar mercadoria {Codigo}.", codigoBarras);
            return ResultadoOperacao.Falha(
                $"Já existe uma mercadoria com o código de barras {codigoBarras}.");
        }

        _logger.LogInformation("Mercadoria cadastrada: {Nome} (Id {Id}).", mercadoria.Nome, mercadoria.Id);
        return ResultadoOperacao.Ok(mercadoria.Id);
    }

    public async Task<ResultadoOperacao> AtualizarAsync(
        int id, CadastroMercadoriaDados dados, CancellationToken cancellationToken = default)
    {
        var erros = ValidarCampos(dados, out var nome, out var codigoBarras);

        // Ignora o próprio registro na checagem de código único.
        if (codigoBarras is not null &&
            await _repositorio.CodigoBarrasExisteAsync(codigoBarras, id, cancellationToken))
            erros.Add($"Já existe uma mercadoria com o código de barras {codigoBarras}.");

        if (erros.Count > 0)
            return ResultadoOperacao.Falha(erros);

        var mercadoria = await _repositorio.GetByIdAsync(id);
        if (mercadoria is null || !mercadoria.Ativo)
            return ResultadoOperacao.Falha("Mercadoria não encontrada.");

        AplicarDados(mercadoria, dados, nome, codigoBarras);

        try
        {
            await _repositorio.UpdateAsync(mercadoria, cancellationToken);
        }
        catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
        {
            _logger.LogWarning(ex, "Violação de unicidade ao editar mercadoria {Id}.", id);
            return ResultadoOperacao.Falha(
                $"Já existe uma mercadoria com o código de barras {codigoBarras}.");
        }

        _logger.LogInformation("Mercadoria atualizada: {Nome} (Id {Id}).", mercadoria.Nome, id);
        return ResultadoOperacao.Ok(id);
    }

    /// <summary>Exclusão lógica: marca Ativo = 0, preservando o registro físico.</summary>
    public async Task<ResultadoOperacao> ExcluirAsync(int id, CancellationToken cancellationToken = default)
    {
        var mercadoria = await _repositorio.GetByIdAsync(id);
        if (mercadoria is null || !mercadoria.Ativo)
            return ResultadoOperacao.Falha("Mercadoria não encontrada.");

        mercadoria.Ativo = false;
        await _repositorio.UpdateAsync(mercadoria, cancellationToken);

        _logger.LogInformation("Mercadoria excluída logicamente: {Nome} (Id {Id}).", mercadoria.Nome, id);
        return ResultadoOperacao.Ok(id);
    }

    // ----- Auxiliares -----

    private static List<string> ValidarCampos(
        CadastroMercadoriaDados dados, out string nome, out string? codigoBarras)
    {
        var erros = new List<string>();

        nome = dados.Nome?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nome))
            erros.Add("O nome é obrigatório.");
        if (dados.PrecoCustoReais < 0)
            erros.Add("O preço de custo não pode ser negativo.");
        if (dados.PrecoVendaReais < 0)
            erros.Add("O preço de venda não pode ser negativo.");
        if (dados.Quantidade < 0)
            erros.Add("A quantidade não pode ser negativa.");

        codigoBarras = string.IsNullOrWhiteSpace(dados.CodigoBarras) ? null : dados.CodigoBarras.Trim();
        return erros;
    }

    private static void AplicarDados(
        Mercadoria mercadoria, CadastroMercadoriaDados dados, string nome, string? codigoBarras)
    {
        mercadoria.Nome = nome;
        mercadoria.Fornecedor = string.IsNullOrWhiteSpace(dados.Fornecedor) ? null : dados.Fornecedor.Trim();
        mercadoria.Unidade = dados.Unidade;
        mercadoria.PrecoCusto = Moeda.ParaCentavos(dados.PrecoCustoReais);
        mercadoria.PrecoVenda = Moeda.ParaCentavos(dados.PrecoVendaReais);
        mercadoria.CodigoBarras = codigoBarras;

        // Regra do domínio: itens por peso não têm estoque nem validade — zera aqui,
        // independentemente do que a tela mandar.
        var porPeso = dados.Unidade == UnidadeMedida.Quilo;
        mercadoria.Quantidade = porPeso ? 0 : dados.Quantidade;
        mercadoria.Validade = porPeso ? null : dados.Validade;
    }

    private static bool EhViolacaoUnicidade(DbUpdateException ex)
        => ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraint };
}
