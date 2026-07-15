using Market.Application.Services;
using Market.Domain;
using Market.Infrastructure.Data;
using Market.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

public class VendaServiceTests
{
    private const string CpfValido = "52998224725";

    private static VendaService CriarServico(BancoDeTeste banco)
        => new(banco, NullLogger<VendaService>.Instance);

    // T12 — venda com sucesso
    [Fact]
    public async Task Venda_com_sucesso_baixa_estoque_congela_precos_e_soma_total()
    {
        using var banco = new BancoDeTeste();
        var arroz = banco.CriarMercadoria(estoque: 50, precoVenda: 2500, precoCusto: 1800, nome: "Arroz");
        var feijao = banco.CriarMercadoria(estoque: 80, precoVenda: 790, precoCusto: 500, nome: "Feijão");
        var servico = CriarServico(banco);

        var resultado = await servico.FinalizarVendaAsync(null, new[]
        {
            new ItemCarrinho(arroz.Id, 2),
            new ItemCarrinho(feijao.Id, 3)
        });

        Assert.True(resultado.Sucesso);
        Assert.Equal(48, banco.EstoqueAtual(arroz.Id));
        Assert.Equal(77, banco.EstoqueAtual(feijao.Id));

        using var context = banco.CreateDbContext();
        var venda = context.Vendas.Include(v => v.Itens).Single();
        Assert.Equal(2 * 2500 + 3 * 790, venda.ValorTotal);
        Assert.Equal(2, venda.Itens.Count);
        var itemArroz = venda.Itens.Single(i => i.MercadoriaId == arroz.Id);
        Assert.Equal(2500, itemArroz.PrecoUnitario);
        Assert.Equal(1800, itemArroz.PrecoCusto);
    }

    // T13 — estoque insuficiente
    [Fact]
    public async Task Estoque_insuficiente_falha_com_mensagem()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 3, nome: "Leite");
        var servico = CriarServico(banco);

        var resultado = await servico.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 5) });

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("Leite") && e.Contains("Disponível: 3"));
    }

    // T14 — rollback: item 1 válido + item 2 sem estoque => nada persiste
    [Fact]
    public async Task Falha_no_segundo_item_faz_rollback_total()
    {
        using var banco = new BancoDeTeste();
        var ok = banco.CriarMercadoria(estoque: 10, nome: "Item OK");
        var semEstoque = banco.CriarMercadoria(estoque: 1, nome: "Item Sem Estoque");
        var servico = CriarServico(banco);

        var resultado = await servico.FinalizarVendaAsync(null, new[]
        {
            new ItemCarrinho(ok.Id, 2),
            new ItemCarrinho(semEstoque.Id, 5)
        });

        Assert.False(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        Assert.Equal(0, context.Vendas.Count());        // nenhuma venda
        Assert.Equal(0, context.ItensVenda.Count());    // nenhum item
        Assert.Equal(10, banco.EstoqueAtual(ok.Id));    // estoque do 1º item intacto
    }

    // T15 — carrinho vazio
    [Fact]
    public async Task Carrinho_vazio_falha()
    {
        using var banco = new BancoDeTeste();
        var resultado = await CriarServico(banco).FinalizarVendaAsync(null, Array.Empty<ItemCarrinho>());
        Assert.False(resultado.Sucesso);
    }

    // T16 — quantidade <= 0
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Quantidade_nao_positiva_falha(int qtd)
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10);
        var resultado = await CriarServico(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, qtd) });
        Assert.False(resultado.Sucesso);
    }

    // T17 — mercadoria inexistente ou inativa
    [Fact]
    public async Task Mercadoria_inexistente_falha_sem_persistir()
    {
        using var banco = new BancoDeTeste();
        var resultado = await CriarServico(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(999, 1) });

        Assert.False(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        Assert.Equal(0, context.Vendas.Count());
    }

    [Fact]
    public async Task Mercadoria_inativa_falha()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10, ativo: false, nome: "Desativado");
        var resultado = await CriarServico(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) });
        Assert.False(resultado.Sucesso);
    }

    // T18 — cliente informado inexistente
    [Fact]
    public async Task Cliente_inexistente_falha_antes_de_tocar_estoque()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10);
        var resultado = await CriarServico(banco).FinalizarVendaAsync(CpfValido, new[] { new ItemCarrinho(m.Id, 1) });

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("Cliente"));
        Assert.Equal(10, banco.EstoqueAtual(m.Id)); // estoque intacto
    }

    // T19 — venda sem cliente
    [Fact]
    public async Task Venda_sem_cliente_persiste_com_cpf_nulo()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10);
        var resultado = await CriarServico(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) });

        Assert.True(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        Assert.Null(context.Vendas.Single().ClienteCpf);
    }

    [Fact]
    public async Task Venda_com_cliente_valido_registra_cpf()
    {
        using var banco = new BancoDeTeste();
        banco.CriarCliente(CpfValido, "Maria");
        var m = banco.CriarMercadoria(estoque: 10);

        var resultado = await CriarServico(banco).FinalizarVendaAsync("529.982.247-25", new[] { new ItemCarrinho(m.Id, 1) });

        Assert.True(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        Assert.Equal(CpfValido, context.Vendas.Single().ClienteCpf);
    }

    [Fact]
    public async Task Venda_persiste_a_forma_de_pagamento()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 100);
        await CriarServico(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) }, FormaPagamento.Pix);

        using var context = banco.CreateDbContext();
        Assert.Equal(FormaPagamento.Pix, context.Vendas.Single().Forma);
    }

    [Fact]
    public async Task Forma_padrao_e_dinheiro()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 100);
        await CriarServico(banco).FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) });

        using var context = banco.CreateDbContext();
        Assert.Equal(FormaPagamento.Dinheiro, context.Vendas.Single().Forma);
    }

    // T20 — duas linhas do mesmo produto excedendo o estoque somado
    [Fact]
    public async Task Linhas_repetidas_do_mesmo_produto_baixam_cumulativamente()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 5, nome: "Sabão");
        var servico = CriarServico(banco);

        // 3 + 3 = 6 > 5 disponível
        var resultado = await servico.FinalizarVendaAsync(null, new[]
        {
            new ItemCarrinho(m.Id, 3),
            new ItemCarrinho(m.Id, 3)
        });

        Assert.False(resultado.Sucesso);
        Assert.Equal(5, banco.EstoqueAtual(m.Id)); // rollback preserva estoque
    }

    [Fact]
    public async Task Linhas_repetidas_dentro_do_estoque_somam_a_baixa()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 5, precoVenda: 100, nome: "Sabão");
        var servico = CriarServico(banco);

        var resultado = await servico.FinalizarVendaAsync(null, new[]
        {
            new ItemCarrinho(m.Id, 2),
            new ItemCarrinho(m.Id, 2)
        });

        Assert.True(resultado.Sucesso);
        Assert.Equal(1, banco.EstoqueAtual(m.Id)); // 5 - 2 - 2
    }

    // T21 — preço alterado após a venda não muda o item (preço congelado)
    [Fact]
    public async Task Item_mantem_preco_congelado_apos_alteracao_de_cadastro()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 1000, precoCusto: 600, nome: "Café");
        var servico = CriarServico(banco);
        await servico.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) });

        // Altera o preço de cadastro depois da venda.
        using (var context = banco.CreateDbContext())
        {
            var mercadoria = context.Mercadorias.First(x => x.Id == m.Id);
            mercadoria.PrecoVenda = 9999;
            mercadoria.PrecoCusto = 8888;
            context.SaveChanges();
        }

        using var verificacao = banco.CreateDbContext();
        var item = verificacao.ItensVenda.Single();
        Assert.Equal(1000, item.PrecoUnitario); // congelado
        Assert.Equal(600, item.PrecoCusto);
    }
}
