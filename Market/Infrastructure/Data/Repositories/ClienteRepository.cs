using Market.Domain;
using Market.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Market.Infrastructure.Data.Repositories;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ClienteRepository(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
        => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<Cliente>> BuscarAsync(string? cpf, string? nome,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(cpf))
            query = query.Where(c => c.Cpf == cpf);
        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(c => EF.Functions.Like(c.Nome, $"%{nome}%"));

        return await query.OrderBy(c => c.Nome).ToListAsync(cancellationToken);
    }

    public async Task<bool> CpfExisteAsync(string cpf, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Clientes.AnyAsync(c => c.Cpf == cpf, cancellationToken);
    }
}
