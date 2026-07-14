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

    public async Task<IReadOnlyList<Mercadoria>> ListarAsync(FiltroMercadoria filtro,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Mercadorias.AsNoTracking().Where(m => m.Ativo);

        // WHERE dinâmico: cada critério nulo é ignorado.
        if (!string.IsNullOrWhiteSpace(filtro.Nome))
            query = query.Where(m => m.Nome.Contains(filtro.Nome));
        if (!string.IsNullOrWhiteSpace(filtro.Fornecedor))
            query = query.Where(m => m.Fornecedor != null && m.Fornecedor.Contains(filtro.Fornecedor));
        if (!string.IsNullOrWhiteSpace(filtro.CodigoBarras))
            query = query.Where(m => m.CodigoBarras == filtro.CodigoBarras);
        if (filtro.PrecoMinCentavos is int precoMin)
            query = query.Where(m => m.PrecoVenda >= precoMin);
        if (filtro.PrecoMaxCentavos is int precoMax)
            query = query.Where(m => m.PrecoVenda <= precoMax);
        if (filtro.QtdMin is int qtdMin)
            query = query.Where(m => m.Quantidade >= qtdMin);
        if (filtro.ValidadeIni is DateOnly validadeIni)
            query = query.Where(m => m.Validade != null && m.Validade >= validadeIni);
        if (filtro.ValidadeFim is DateOnly validadeFim)
            query = query.Where(m => m.Validade != null && m.Validade <= validadeFim);

        return await query.OrderBy(m => m.Nome).ToListAsync(cancellationToken);
    }
}
