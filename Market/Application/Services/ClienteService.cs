using Market.Domain;
using Market.Domain.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Application.Services;

/// <summary>
/// Regras de negócio de clientes: normalização e validação de CPF (dígito verificador),
/// nome obrigatório, unicidade e busca.
/// </summary>
public class ClienteService
{
    private const int SqliteConstraint = 19; // SQLITE_CONSTRAINT

    private readonly IClienteRepository _repositorio;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(IClienteRepository repositorio, ILogger<ClienteService> logger)
    {
        _repositorio = repositorio;
        _logger = logger;
    }

    public Task<IReadOnlyList<Cliente>> BuscarAsync(
        string? cpf, string? nome, CancellationToken cancellationToken = default)
        => _repositorio.BuscarAsync(Cpf.Normalizar(cpf), nome, cancellationToken);

    public async Task<ResultadoOperacao> CadastrarAsync(
        string? cpf, string? nome, string? contato = null, CancellationToken cancellationToken = default)
    {
        var erros = new List<string>();

        var cpfNormalizado = Cpf.Normalizar(cpf);
        if (!Cpf.EhValido(cpfNormalizado))
            erros.Add("CPF inválido.");

        var nomeNormalizado = nome?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nomeNormalizado))
            erros.Add("O nome é obrigatório.");

        var contatoNormalizado = Contato.Normalizar(contato);
        if (!Contato.EhValido(contato))
            erros.Add("Contato inválido. Informe um telefone ou e-mail.");

        if (erros.Count == 0 &&
            await _repositorio.CpfExisteAsync(cpfNormalizado, cancellationToken))
            erros.Add($"Já existe um cliente com o CPF {cpfNormalizado}.");

        if (erros.Count > 0)
            return ResultadoOperacao.Falha(erros);

        var cliente = new Cliente { Cpf = cpfNormalizado, Nome = nomeNormalizado, Contato = contatoNormalizado };

        try
        {
            await _repositorio.AddAsync(cliente, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException
                                           { SqliteErrorCode: SqliteConstraint })
        {
            _logger.LogWarning(ex, "Violação de unicidade ao cadastrar cliente {Cpf}.", cpfNormalizado);
            return ResultadoOperacao.Falha($"Já existe um cliente com o CPF {cpfNormalizado}.");
        }

        _logger.LogInformation("Cliente cadastrado: {Nome} ({Cpf}).", nomeNormalizado, cpfNormalizado);
        return ResultadoOperacao.Ok();
    }

    /// <summary>Atualiza nome e contato de um cliente existente (o CPF é a chave e não muda).</summary>
    public async Task<ResultadoOperacao> AtualizarAsync(
        string? cpf, string? nome, string? contato, CancellationToken cancellationToken = default)
    {
        var erros = new List<string>();

        var cpfNormalizado = Cpf.Normalizar(cpf);
        if (!Cpf.EhValido(cpfNormalizado))
            erros.Add("CPF inválido.");

        var nomeNormalizado = nome?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nomeNormalizado))
            erros.Add("O nome é obrigatório.");

        var contatoNormalizado = Contato.Normalizar(contato);
        if (!Contato.EhValido(contato))
            erros.Add("Contato inválido. Informe um telefone ou e-mail.");

        if (erros.Count == 0 &&
            !await _repositorio.CpfExisteAsync(cpfNormalizado, cancellationToken))
            erros.Add("Cliente não encontrado.");

        if (erros.Count > 0)
            return ResultadoOperacao.Falha(erros);

        var cliente = new Cliente { Cpf = cpfNormalizado, Nome = nomeNormalizado, Contato = contatoNormalizado };
        await _repositorio.UpdateAsync(cliente, cancellationToken);

        _logger.LogInformation("Cliente atualizado: {Nome} ({Cpf}).", nomeNormalizado, cpfNormalizado);
        return ResultadoOperacao.Ok();
    }
}
