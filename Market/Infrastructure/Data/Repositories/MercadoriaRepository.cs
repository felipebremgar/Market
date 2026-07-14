using Market.Domain;
using Market.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Market.Infrastructure.Data.Repositories;

public class MercadoriaRepository : Repository<Mercadoria>, IMercadoriaRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public MercadoriaRepository(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
        => _contextFactory = contextFactory;

    public async Task<bool> CodigoBarrasExisteAsync(string codigoBarras, int? ignorarId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Mercadorias
            .AnyAsync(m => m.CodigoBarras == codigoBarras
                        && (ignorarId == null || m.Id != ignorarId), cancellationToken);
    }

    public async Task<Mercadoria?> ObterPorCodigoBarrasAsync(string codigoBarras,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Mercadorias
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CodigoBarras == codigoBarras && m.Ativo, cancellationToken);
    }
}
