using Market.Domain;
using Market.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

/// <summary>
/// Relatório de lucro por período, calculado sobre os valores CONGELADOS do ItemVenda
/// (PrecoUnitario e PrecoCusto no momento da venda), não sobre o cadastro atual.
/// Somas em <c>long</c> para não estourar em períodos longos.
/// </summary>
public class RelatorioService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public RelatorioService(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<ResumoLucro> ResumoAsync(
        DateOnly? dataIni, DateOnly? dataFim, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var agregado = await ItensNoPeriodo(context, dataIni, dataFim)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Custo = g.Sum(i => (long)i.Quantidade * i.PrecoCusto),
                Receita = g.Sum(i => (long)i.Quantidade * i.PrecoUnitario)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return agregado is null ? ResumoLucro.Vazio : new ResumoLucro(agregado.Custo, agregado.Receita);
    }

    public async Task<IReadOnlyList<LucroPorProduto>> PorProdutoAsync(
        DateOnly? dataIni, DateOnly? dataFim, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var linhas = await ItensNoPeriodo(context, dataIni, dataFim)
            .GroupBy(i => new { i.MercadoriaId, i.Mercadoria.Nome })
            .Select(g => new
            {
                g.Key.Nome,
                Qtd = g.Sum(i => i.Quantidade),
                Custo = g.Sum(i => (long)i.Quantidade * i.PrecoCusto),
                Receita = g.Sum(i => (long)i.Quantidade * i.PrecoUnitario)
            })
            .OrderByDescending(x => x.Receita - x.Custo)
            .ToListAsync(cancellationToken);

        return linhas.Select(x => new LucroPorProduto(x.Nome, x.Qtd, x.Custo, x.Receita)).ToList();
    }

    public async Task<IReadOnlyList<LucroPorDia>> PorDiaAsync(
        DateOnly? dataIni, DateOnly? dataFim, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Agrupamento por dia feito em memória: date() sobre a coluna com value converter
        // não é traduzível; o volume de itens do período é gerenciável.
        var itens = await ItensNoPeriodo(context, dataIni, dataFim)
            .Select(i => new
            {
                i.Venda.DataVenda,
                Custo = (long)i.Quantidade * i.PrecoCusto,
                Receita = (long)i.Quantidade * i.PrecoUnitario
            })
            .ToListAsync(cancellationToken);

        return itens
            .GroupBy(x => DateOnly.FromDateTime(x.DataVenda))
            .OrderBy(g => g.Key)
            .Select(g => new LucroPorDia(g.Key, g.Sum(x => x.Receita), g.Sum(x => x.Custo)))
            .ToList();
    }

    private static IQueryable<ItemVenda> ItensNoPeriodo(
        AppDbContext context, DateOnly? dataIni, DateOnly? dataFim)
    {
        var query = context.ItensVenda.AsNoTracking().AsQueryable();

        if (dataIni is DateOnly ini)
            query = query.Where(i => i.Venda.DataVenda >= ini.ToDateTime(TimeOnly.MinValue));
        if (dataFim is DateOnly fim)
            query = query.Where(i => i.Venda.DataVenda < fim.AddDays(1).ToDateTime(TimeOnly.MinValue));

        return query;
    }
}
