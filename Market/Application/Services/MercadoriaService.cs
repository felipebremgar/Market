using Market.Domain;
using Market.Domain.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Application.Services;

/// <summary>
/// Regras de negócio do cadastro de mercadorias, independentes da UI:
/// validação, conversão de preços para centavos e checagem de código de barras único.
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

    public async Task<ResultadoOperacao> CadastrarAsync(
        CadastroMercadoriaDados dados, CancellationToken cancellationToken = default)
    {
        var erros = new List<string>();

        var nome = dados.Nome?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nome))
            erros.Add("O nome é obrigatório.");
        if (dados.PrecoCustoReais < 0)
            erros.Add("O preço de custo não pode ser negativo.");
        if (dados.PrecoVendaReais < 0)
            erros.Add("O preço de venda não pode ser negativo.");
        if (dados.Quantidade < 0)
            erros.Add("A quantidade não pode ser negativa.");

        var codigoBarras = string.IsNullOrWhiteSpace(dados.CodigoBarras)
            ? null : dados.CodigoBarras.Trim();

        if (codigoBarras is not null &&
            await _repositorio.CodigoBarrasExisteAsync(codigoBarras, null, cancellationToken))
            erros.Add($"Já existe uma mercadoria com o código de barras {codigoBarras}.");

        if (erros.Count > 0)
            return ResultadoOperacao.Falha(erros);

        var mercadoria = new Mercadoria
        {
            Nome = nome,
            Fornecedor = string.IsNullOrWhiteSpace(dados.Fornecedor) ? null : dados.Fornecedor.Trim(),
            PrecoCusto = Moeda.ParaCentavos(dados.PrecoCustoReais),
            PrecoVenda = Moeda.ParaCentavos(dados.PrecoVendaReais),
            Quantidade = dados.Quantidade,
            CodigoBarras = codigoBarras,
            Validade = dados.Validade,
            Ativo = true
        };

        try
        {
            await _repositorio.AddAsync(mercadoria, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException
                                           { SqliteErrorCode: SqliteConstraint })
        {
            // Rede de segurança contra corrida entre a checagem e a inserção:
            // o índice único UQ_Mercadoria_CodigoBarras barra o duplicado no banco.
            _logger.LogWarning(ex, "Violação de unicidade ao cadastrar mercadoria {Codigo}.", codigoBarras);
            return ResultadoOperacao.Falha(
                $"Já existe uma mercadoria com o código de barras {codigoBarras}.");
        }

        _logger.LogInformation("Mercadoria cadastrada: {Nome} (Id {Id}).", mercadoria.Nome, mercadoria.Id);
        return ResultadoOperacao.Ok(mercadoria.Id);
    }
}
