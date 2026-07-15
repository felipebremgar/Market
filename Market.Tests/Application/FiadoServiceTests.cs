using Market.Application.Services;
using Market.Domain;
using Market.Tests.Infra;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

public class FiadoServiceTests
{
    private const string Cpf = "52998224725";

    private static (FiadoService fiado, VendaService venda) Criar(BancoDeTeste banco)
        => (new FiadoService(banco, NullLogger<FiadoService>.Instance),
            new VendaService(banco, NullLogger<VendaService>.Instance));

    [Fact]
    public async Task Lista_pendentes_ordenados_por_vencimento_e_ignora_a_vista()
    {
        using var banco = new BancoDeTeste();
        var (fiado, venda) = Criar(banco);
        banco.CriarCliente(Cpf, "Maria");
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 1000, precoCusto: 600);

        await venda.FinalizarVendaAsync(Cpf, new[] { new ItemCarrinho(m.Id, 1) },
            FormaPagamento.Fiado, DateOnly.FromDateTime(DateTime.Today.AddDays(20)));
        await venda.FinalizarVendaAsync(Cpf, new[] { new ItemCarrinho(m.Id, 1) },
            FormaPagamento.Fiado, DateOnly.FromDateTime(DateTime.Today.AddDays(5)));
        await venda.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) }, FormaPagamento.Dinheiro);

        var pendentes = await fiado.ListarPendentesAsync();

        Assert.Equal(2, pendentes.Count); // à vista não entra
        Assert.True(pendentes[0].DataVencimento < pendentes[1].DataVencimento); // vencimento mais próximo primeiro
    }

    [Fact]
    public async Task Baixa_marca_paga_com_data_e_sai_da_lista()
    {
        using var banco = new BancoDeTeste();
        var (fiado, venda) = Criar(banco);
        banco.CriarCliente(Cpf, "Maria");
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 1000, precoCusto: 600);
        var r = await venda.FinalizarVendaAsync(Cpf, new[] { new ItemCarrinho(m.Id, 1) },
            FormaPagamento.Fiado, DateOnly.FromDateTime(DateTime.Today.AddDays(10)));

        var resultado = await fiado.DarBaixaAsync(r.IdGerado!.Value);

        Assert.True(resultado.Sucesso);
        using var context = banco.CreateDbContext();
        var vendaDb = context.Vendas.Single();
        Assert.Equal(StatusPagamento.Pago, vendaDb.Status);
        Assert.NotNull(vendaDb.DataBaixa);
        Assert.Empty(await fiado.ListarPendentesAsync());
    }

    [Fact]
    public async Task Baixa_em_venda_ja_paga_falha()
    {
        using var banco = new BancoDeTeste();
        var (fiado, venda) = Criar(banco);
        banco.CriarCliente(Cpf, "Maria");
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 1000, precoCusto: 600);
        var r = await venda.FinalizarVendaAsync(Cpf, new[] { new ItemCarrinho(m.Id, 1) },
            FormaPagamento.Fiado, DateOnly.FromDateTime(DateTime.Today.AddDays(10)));
        await fiado.DarBaixaAsync(r.IdGerado!.Value);

        var segunda = await fiado.DarBaixaAsync(r.IdGerado!.Value);

        Assert.False(segunda.Sucesso);
    }

    [Fact]
    public async Task Baixa_em_venda_a_vista_falha()
    {
        using var banco = new BancoDeTeste();
        var (fiado, venda) = Criar(banco);
        var m = banco.CriarMercadoria(estoque: 10, precoVenda: 1000, precoCusto: 600);
        var r = await venda.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1) }, FormaPagamento.Dinheiro);

        var resultado = await fiado.DarBaixaAsync(r.IdGerado!.Value);

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("não é fiada"));
    }
}
