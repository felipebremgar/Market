using Market.Application.Services;
using Market.Domain;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

/// <summary>
/// v2.4 — verduras/frutas vendidas por quilo: sem acompanhamento de estoque nem validade,
/// preços por kg e peso em gramas.
/// </summary>
public class MercadoriaPorPesoTests
{
    private static MercadoriaService CriarCadastro(BancoDeTeste banco)
        => new(new MercadoriaRepository(banco), NullLogger<MercadoriaService>.Instance);

    private static VendaService CriarVendas(BancoDeTeste banco)
        => new(banco, NullLogger<VendaService>.Instance);

    [Fact]
    public async Task Cadastro_por_quilo_ignora_quantidade_e_validade()
    {
        using var banco = new BancoDeTeste();

        var resultado = await CriarCadastro(banco).CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Tomate",
            Unidade = UnidadeMedida.Quilo,
            PrecoCustoReais = 12.00m,
            PrecoVendaReais = 19.90m,
            Quantidade = 50,                                                // deve ser ignorada
            Validade = DateOnly.FromDateTime(DateTime.Today.AddDays(10))    // deve ser ignorada
        });

        Assert.True(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        var tomate = context.Mercadorias.Single();
        Assert.Equal(UnidadeMedida.Quilo, tomate.Unidade);
        Assert.Equal(0, tomate.Quantidade);
        Assert.Null(tomate.Validade);
        Assert.Equal(1990, tomate.PrecoVenda);   // R$19,90 por kg
    }

    [Fact]
    public async Task Cadastro_por_unidade_mantem_quantidade_e_validade()
    {
        using var banco = new BancoDeTeste();
        var validade = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        await CriarCadastro(banco).CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Arroz",
            Unidade = UnidadeMedida.Unidade,
            PrecoVendaReais = 25.00m,
            Quantidade = 50,
            Validade = validade
        });

        using var context = banco.CreateDbContext();
        var arroz = context.Mercadorias.Single();
        Assert.Equal(50, arroz.Quantidade);
        Assert.Equal(validade, arroz.Validade);
    }

    [Fact]
    public async Task Venda_por_peso_congela_totais_e_nao_toca_no_estoque()
    {
        using var banco = new BancoDeTeste();
        // Estoque 0 de propósito: item por peso não tem acompanhamento, a venda deve passar.
        var tomate = banco.CriarMercadoria(
            estoque: 0, precoVenda: 1990, precoCusto: 1200, nome: "Tomate",
            unidade: UnidadeMedida.Quilo);

        // 750 g a R$19,90/kg = R$14,925 → R$14,93
        var resultado = await CriarVendas(banco).FinalizarVendaAsync(
            null, new[] { new ItemCarrinho(tomate.Id, 750) });

        Assert.True(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        var item = context.ItensVenda.Single();
        Assert.Equal(UnidadeMedida.Quilo, item.Unidade);
        Assert.Equal(750, item.Quantidade);          // gramas
        Assert.Equal(1990, item.PrecoUnitario);      // preço por kg congelado
        Assert.Equal(1493, item.SubtotalCentavos);   // total congelado, arredondado
        Assert.Equal(900, item.CustoCentavos);       // 750 × 1200 / 1000
        Assert.Equal(1493, context.Vendas.Single().ValorTotal);
        Assert.Equal(0, banco.EstoqueAtual(tomate.Id));   // não decrementou
    }

    [Fact]
    public async Task Venda_por_unidade_continua_baixando_estoque()
    {
        using var banco = new BancoDeTeste();
        var arroz = banco.CriarMercadoria(estoque: 10, precoVenda: 2500, precoCusto: 1800, nome: "Arroz");

        await CriarVendas(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(arroz.Id, 2) });

        Assert.Equal(8, banco.EstoqueAtual(arroz.Id));
        using var context = banco.CreateDbContext();
        var item = context.ItensVenda.Single();
        Assert.Equal(5000, item.SubtotalCentavos);
        Assert.Equal(3600, item.CustoCentavos);
    }

    /// <summary>
    /// O ponto central do desenho: o relatório soma os totais congelados, então bate
    /// exatamente com o valor da venda/recibo — sem divergir por arredondamento.
    /// </summary>
    [Fact]
    public async Task Relatorio_bate_exatamente_com_o_total_da_venda_por_peso()
    {
        using var banco = new BancoDeTeste();
        var tomate = banco.CriarMercadoria(
            estoque: 0, precoVenda: 1990, precoCusto: 1200, nome: "Tomate",
            unidade: UnidadeMedida.Quilo);
        var vendas = CriarVendas(banco);
        var r = await vendas.FinalizarVendaAsync(null, new[] { new ItemCarrinho(tomate.Id, 750) });

        var recibo = await vendas.ObterReciboAsync(r.IdGerado!.Value);
        var resumo = await new RelatorioService(banco).ResumoAsync(null, null);

        Assert.Equal(1493, recibo!.TotalCentavos);
        Assert.Equal(recibo.TotalCentavos, resumo.ReceitaTotalCentavos);   // recibo == relatório
        Assert.Equal(900, resumo.CustoTotalCentavos);
    }
}
