using Market.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Market.Infrastructure.Data.Repositories;

/// <summary>
/// Repositório genérico. Cada operação usa um <see cref="AppDbContext"/> novo e de vida
/// curta, obtido do factory — sem contexto compartilhado de longa duração (evita dados
/// obsoletos e problemas de thread no app desktop).
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public Repository(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<T?> GetByIdAsync(params object[] keyValues)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>().FindAsync(keyValues);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
