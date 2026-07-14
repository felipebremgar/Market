namespace Market.Domain.Repositories;

/// <summary>
/// Repositório CRUD assíncrono genérico. Cada método é uma unidade de trabalho
/// completa (abre contexto, executa, salva e fecha), adequado a um app desktop
/// onde não há escopo de requisição.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Busca por chave primária (int Id, ou string Cpf para Cliente).</summary>
    Task<T?> GetByIdAsync(params object[] keyValues);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Insere e persiste; retorna a entidade (com Id preenchido, quando gerado).</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
