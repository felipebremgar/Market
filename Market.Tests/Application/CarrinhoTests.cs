using Market.Application.Services;
using Market.Domain;

namespace Market.Tests.Application;

public class CarrinhoTests
{
    private static Mercadoria Produto(int id, int preco = 1000, int estoque = 10, string nome = "Produto")
        => new() { Id = id, Nome = nome, PrecoVenda = preco, Quantidade = estoque, Ativo = true };

    // T22
    [Fact]
    public void Adicionar_produto_novo_cria_linha_com_preco_do_cadastro()
    {
        var carrinho = new Carrinho();
        var r = carrinho.Adicionar(Produto(1, preco: 2500, nome: "Arroz"));

        Assert.True(r.Sucesso);
        var linha = Assert.Single(carrinho.Linhas);
        Assert.Equal("Arroz", linha.Nome);
        Assert.Equal(2500, linha.PrecoUnitarioCentavos);
        Assert.Equal(1, linha.Quantidade);
    }

    // T23
    [Fact]
    public void Adicionar_o_mesmo_produto_consolida_em_uma_linha()
    {
        var carrinho = new Carrinho();
        var arroz = Produto(1, preco: 2500);
        carrinho.Adicionar(arroz);
        carrinho.Adicionar(arroz);

        var linha = Assert.Single(carrinho.Linhas);
        Assert.Equal(2, linha.Quantidade);
        Assert.Equal(5000, linha.SubtotalCentavos);
    }

    // T24
    [Fact]
    public void Dois_produtos_somam_subtotais_no_total()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar(Produto(1, preco: 2500), 2); // 5000
        carrinho.Adicionar(Produto(2, preco: 790), 3);  // 2370

        Assert.Equal(2, carrinho.Linhas.Count);
        Assert.Equal(7370, carrinho.TotalCentavos);
    }

    // T25
    [Fact]
    public void Alterar_quantidade_recalcula_total()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar(Produto(1, preco: 1000, estoque: 10));

        var r = carrinho.AlterarQuantidade(1, 4);

        Assert.True(r.Sucesso);
        Assert.Equal(4000, carrinho.TotalCentavos);
    }

    // T26
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Quantidade_nao_positiva_e_rejeitada(int qtd)
    {
        var carrinho = new Carrinho();
        var produto = Produto(1);

        Assert.False(carrinho.Adicionar(produto, qtd).Sucesso);
        carrinho.Adicionar(produto);
        Assert.False(carrinho.AlterarQuantidade(1, qtd).Sucesso);
    }

    // T27
    [Fact]
    public void Adicionar_alem_do_estoque_considerando_o_carrinho_e_rejeitado()
    {
        var carrinho = new Carrinho();
        var produto = Produto(1, estoque: 3, nome: "Leite");
        carrinho.Adicionar(produto, 2);

        var r = carrinho.Adicionar(produto, 2); // 2 + 2 = 4 > 3

        Assert.False(r.Sucesso);
        Assert.Contains(r.Erros, e => e.Contains("Leite") && e.Contains("Disponível: 3"));
        Assert.Equal(2, carrinho.Linhas.Single().Quantidade); // não alterou
    }

    // T28
    [Fact]
    public void Remover_linha_atualiza_total()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar(Produto(1, preco: 2500));
        carrinho.Adicionar(Produto(2, preco: 790));

        carrinho.Remover(2);

        Assert.Single(carrinho.Linhas);
        Assert.Equal(2500, carrinho.TotalCentavos);
    }

    // T29
    [Fact]
    public void Limpar_zera_o_carrinho()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar(Produto(1));
        carrinho.Adicionar(Produto(2));

        carrinho.Limpar();

        Assert.True(carrinho.Vazio);
        Assert.Equal(0, carrinho.TotalCentavos);
    }

    // T30
    [Fact]
    public void ParaItensCarrinho_converte_para_o_tipo_do_dia_5()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar(Produto(1), 2);
        carrinho.Adicionar(Produto(2), 3);

        var itens = carrinho.ParaItensCarrinho();

        Assert.Equal(2, itens.Count);
        Assert.Contains(new ItemCarrinho(1, 2), itens);
        Assert.Contains(new ItemCarrinho(2, 3), itens);
    }

    [Fact]
    public void Alterar_acima_do_estoque_e_rejeitado()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar(Produto(1, estoque: 5));

        Assert.False(carrinho.AlterarQuantidade(1, 6).Sucesso);
        Assert.True(carrinho.AlterarQuantidade(1, 5).Sucesso);
    }
}
