using Market.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Market.Tests.Infra;

public class BancoDeTesteSmokeTests
{
    [Fact]
    public void Banco_criado_tem_as_quatro_tabelas()
    {
        using var banco = new BancoDeTeste();
        using var context = banco.CreateDbContext();

        var tabelas = context.Database
            .SqlQuery<string>($"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
            .ToList();

        Assert.Contains("Cliente", tabelas);
        Assert.Contains("Mercadoria", tabelas);
        Assert.Contains("Venda", tabelas);
        Assert.Contains("ItemVenda", tabelas);
    }

    [Fact]
    public void Builder_de_mercadoria_persiste_com_id()
    {
        using var banco = new BancoDeTeste();

        var mercadoria = banco.CriarMercadoria(estoque: 25, precoVenda: 1500);

        Assert.True(mercadoria.Id > 0);
        Assert.Equal(25, banco.EstoqueAtual(mercadoria.Id));
    }
}
