using Market.Domain;
using Market.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

/// <summary>
/// Consulta do histórico de vendas: lista de cabeçalhos com filtros (período, cliente,
/// produto) e detalhamento dos itens de uma venda.
/// </summary>
public class HistoricoService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public HistoricoService(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<VendaResumo>> BuscarVendasAsync(
        FiltroVenda filtro, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Vendas.AsNoTracking().AsQueryable();

        if (filtro.DataIni is DateOnly ini)
            query = query.Where(v => v.DataVenda >= ini.ToDateTime(TimeOnly.MinValue));
        if (filtro.DataFim is DateOnly fim)
            // Dia inteiro incluído: < dia seguinte à meia-noite.
            query = query.Where(v => v.DataVenda < fim.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (!string.IsNullOrWhiteSpace(filtro.ClienteCpf))
            query = query.Where(v => v.ClienteCpf == filtro.ClienteCpf);
        if (!string.IsNullOrWhiteSpace(filtro.ProdutoNome))
            query = query.Where(v => v.Itens.Any(i =>
                EF.Functions.Like(i.Mercadoria.Nome, $"%{filtro.ProdutoNome}%")));

        return await query
            .OrderByDescending(v => v.DataVenda)
            .Select(v => new VendaResumo(
                v.Id, v.DataVenda, v.ValorTotal, v.ClienteCpf,
                v.Cliente != null ? v.Cliente.Nome : null, v.Forma))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReciboItem>> ObterItensAsync(
        int vendaId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ItensVenda.AsNoTracking()
            .Where(i => i.VendaId == vendaId)
            .Select(i => new ReciboItem(i.Mercadoria.Nome, i.Quantidade, i.PrecoUnitario))
            .ToListAsync(cancellationToken);
    }
}
