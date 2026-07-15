using Market.Application.Services;
using Market.Domain;
using Market.Tests.Infra;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

public class ReciboTests
{
    private const string CpfValido = "52998224725";

    private static VendaService CriarServico(BancoDeTeste banco)
        => new(banco, NullLogger<VendaService>.Instance);

    // T35 — recibo de venda com cliente traz itens, total e cliente
    [Fact]
    public async Task Recibo_traz_itens_total_e_cliente()
    {
        using var banco = new BancoDeTeste();
        banco.CriarCliente(CpfValido, "Maria");
        var arroz = banco.CriarMercadoria(estoque: 50, precoVenda: 2500, nome: "Arroz");
        var feijao = banco.CriarMercadoria(estoque: 80, precoVenda: 790, nome: "Feijão");
        var servico = CriarServico(banco);

        var venda = await servico.FinalizarVendaAsync(CpfValido, new[]
        {
            new ItemCarrinho(arroz.Id, 2),
            new ItemCarrinho(feijao.Id, 3)
        });

        var recibo = await servico.ObterReciboAsync(venda.IdGerado!.Value);

        Assert.NotNull(recibo);
        Assert.Equal("Maria", recibo!.ClienteNome);
        Assert.Equal(CpfValido, recibo.ClienteCpf);
        Assert.Equal(2 * 2500 + 3 * 790, recibo.TotalCentavos);
        Assert.Equal(2, recibo.Itens.Count);
        var itemArroz = recibo.Itens.Single(i => i.Nome == "Arroz");
        Assert.Equal(2, itemArroz.Quantidade);
        Assert.Equal(2500, itemArroz.PrecoUnitarioCentavos);
        Assert.Equal(5000, itemArroz.SubtotalCentavos);
    }

    // T36 — venda sem cliente => ClienteNome null
    [Fact]
    public async Task Recibo_de_venda_sem_cliente_tem_cliente_nulo()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10);
        var servico = CriarServico(banco);
        var venda = await servico.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) });

        var recibo = await servico.ObterReciboAsync(venda.IdGerado!.Value);

        Assert.NotNull(recibo);
        Assert.Null(recibo!.ClienteNome);
        Assert.Null(recibo.ClienteCpf);
    }

    // v1.10 — o recibo carrega a forma de pagamento persistida (para reabrir pelo histórico)
    [Fact]
    public async Task Recibo_traz_a_forma_de_pagamento()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 100);
        var servico = CriarServico(banco);
        var venda = await servico.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) }, FormaPagamento.Pix);

        var recibo = await servico.ObterReciboAsync(venda.IdGerado!.Value);

        Assert.Equal(FormaPagamento.Pix, recibo!.Forma);
    }

    // T37 — venda inexistente => null
    [Fact]
    public async Task Recibo_de_venda_inexistente_e_nulo()
    {
        using var banco = new BancoDeTeste();
        var recibo = await CriarServico(banco).ObterReciboAsync(999);
        Assert.Null(recibo);
    }
}
