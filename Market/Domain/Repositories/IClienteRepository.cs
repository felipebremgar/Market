using Market.Domain;

namespace Market.Domain.Repositories;

/// <summary>Repositório de clientes com buscas específicas do domínio.</summary>
public interface IClienteRepository : IRepository<Cliente>
{
    /// <summary>
    /// Busca clientes por CPF exato e/ou nome parcial (critérios nulos são ignorados),
    /// ordenados por nome.
    /// </summary>
    Task<IReadOnlyList<Cliente>> BuscarAsync(string? cpf, string? nome,
        CancellationToken cancellationToken = default);

    Task<bool> CpfExisteAsync(string cpf, CancellationToken cancellationToken = default);
}
