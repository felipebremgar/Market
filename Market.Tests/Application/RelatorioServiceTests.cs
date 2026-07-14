using Market.Application.Services;
using Market.Tests.Infra;

namespace Market.Tests.Application;

public class RelatorioServiceTests
{
    private static RelatorioService CriarServico(BancoDeTeste banco) => new(banco);

    // T44 — resumo: custo, receita e lucro com valores congelados
    [Fact]
    public async Task Resumo_calcula_custo_receita_e_lucro()
    {
        using var banco = new BancoDeTeste();
        var arroz = banco.CriarMercadoria(nome: "Arroz");
        var feijao = banco.CriarMercadoria(nome: "Feijão");
        // Venda: 2 Arroz (venda 2500, custo 1800) + 3 Feijão (venda 790, custo 500)
        banco.CriarVendaComCusto(DateTime.Now, null,
            (arroz.Id, 2, 2500, 1800), (feijao.Id, 3, 790, 500));

        var resumo = await CriarServico(banco).ResumoAsync(null, null);

        Assert.Equal(2L * 1800 + 3 * 500, resumo.CustoTotalCentavos);   // 5100
        Assert.Equal(2L * 2500 + 3 * 790, resumo.ReceitaTotalCentavos); // 7370
        Assert.Equal(7370 - 5100, resumo.LucroTotalCentavos);           // 2270
    }

    // T45 — por produto: agrupa e ordena por lucro desc
    [Fact]
    public async Task Por_produto_agrupa_e_ordena_por_lucro_desc()
    {
        using var banco = new BancoDeTeste();
        var arroz = banco.CriarMercadoria(nome: "Arroz");
        var feijao = banco.CriarMercadoria(nome: "Feijão");
        banco.CriarVendaComCusto(DateTime.Now, null,
            (arroz.Id, 2, 2500, 1800),  // lucro 2*(700)=1400
            (feijao.Id, 3, 790, 500));  // lucro 3*(290)=870
        banco.CriarVendaComCusto(DateTime.Now, null, (arroz.Id, 1, 2500, 1800)); // + lucro 700

        var porProduto = await CriarServico(banco).PorProdutoAsync(null, null);

        Assert.Equal(2, porProduto.Count);
        Assert.Equal("Arroz", porProduto[0].Nome);          // maior lucro primeiro (2100)
        Assert.Equal(3, porProduto[0].QtdVendida);
        Assert.Equal(2100, porProduto[0].LucroCentavos);
        Assert.Equal(870, porProduto[1].LucroCentavos);
    }

    // T46 — por dia
    [Fact]
    public async Task Por_dia_agrupa_por_data()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria();
        banco.CriarVendaComCusto(new DateTime(2026, 7, 10, 9, 0, 0), null, (m.Id, 1, 1000, 600));
        banco.CriarVendaComCusto(new DateTime(2026, 7, 10, 18, 0, 0), null, (m.Id, 2, 1000, 600));
        banco.CriarVendaComCusto(new DateTime(2026, 7, 12, 9, 0, 0), null, (m.Id, 1, 1000, 600));

        var porDia = await CriarServico(banco).PorDiaAsync(null, null);

        Assert.Equal(2, porDia.Count);
        Assert.Equal(new DateOnly(2026, 7, 10), porDia[0].Dia);
        Assert.Equal(3000, porDia[0].ReceitaCentavos); // 1+2 unidades no dia 10
        Assert.Equal(1800, porDia[0].CustoCentavos);
        Assert.Equal(1200, porDia[0].LucroCentavos);
    }

    // T47 — filtro de período restringe
    [Fact]
    public async Task Filtro_de_periodo_restringe_o_resumo()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria();
        banco.CriarVendaComCusto(new DateTime(2026, 7, 5, 9, 0, 0), null, (m.Id, 1, 1000, 600));
        banco.CriarVendaComCusto(new DateTime(2026, 7, 14, 9, 0, 0), null, (m.Id, 1, 1000, 600));

        var resumo = await CriarServico(banco).ResumoAsync(
            new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 20));

        Assert.Equal(1000, resumo.ReceitaTotalCentavos); // só a venda do dia 14
    }

    // T48 — usa preços CONGELADOS, não o cadastro atual
    [Fact]
    public async Task Usa_precos_congelados_e_nao_o_cadastro_atual()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(nome: "Café", precoVenda: 1000, precoCusto: 600);
        banco.CriarVendaComCusto(DateTime.Now, null, (m.Id, 1, 1000, 600)); // congelado

        // Altera o cadastro depois da venda
        using (var ctx = banco.CreateDbContext())
        {
            var cafe = ctx.Mercadorias.First(x => x.Id == m.Id);
            cafe.PrecoVenda = 9999; cafe.PrecoCusto = 8888;
            ctx.SaveChanges();
        }

        var resumo = await CriarServico(banco).ResumoAsync(null, null);

        Assert.Equal(1000, resumo.ReceitaTotalCentavos); // preço congelado, não 9999
        Assert.Equal(600, resumo.CustoTotalCentavos);
    }

    // T49 — período sem vendas => zeros
    [Fact]
    public async Task Periodo_sem_vendas_retorna_zeros()
    {
        using var banco = new BancoDeTeste();
        var resumo = await CriarServico(banco).ResumoAsync(
            new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31));

        Assert.Equal(0, resumo.CustoTotalCentavos);
        Assert.Equal(0, resumo.ReceitaTotalCentavos);
        Assert.Equal(0, resumo.LucroTotalCentavos);
        Assert.Empty(await CriarServico(banco).PorProdutoAsync(null, null));
    }
}
