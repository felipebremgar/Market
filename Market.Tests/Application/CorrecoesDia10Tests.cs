using Market.Application.Services;
using Market.Domain;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

public class CorrecoesDia10Tests
{
    private static MercadoriaService CriarMercadoriaService(BancoDeTeste banco)
        => new(new MercadoriaRepository(banco), NullLogger<MercadoriaService>.Instance);

    private static VendaService CriarVendaService(BancoDeTeste banco)
        => new(banco, NullLogger<VendaService>.Instance);

    // Pendência 1 — filtro de mercadoria case-insensitive
    [Theory]
    [InlineData("arroz")]
    [InlineData("ARROZ")]
    [InlineData("Arroz")]
    public async Task Filtro_por_nome_e_case_insensitive(string termo)
    {
        using var banco = new BancoDeTeste();
        banco.CriarMercadoria(nome: "Arroz 5kg");
        var repo = new MercadoriaRepository(banco);

        var achados = await repo.ListarAsync(new FiltroMercadoria { Nome = termo });

        Assert.Single(achados);
        Assert.Equal("Arroz 5kg", achados[0].Nome);
    }

    // Pendência 3 — código de barras de item inativo reativa em vez de barrar
    [Fact]
    public async Task Cadastrar_codigo_de_item_inativo_reativa_o_registro()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarMercadoriaService(banco);
        var criado = await servico.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Arroz", PrecoVendaReais = 25m, Quantidade = 10, CodigoBarras = "7891234567890"
        });
        await servico.ExcluirAsync(criado.IdGerado!.Value); // exclusão lógica

        // Recadastra o mesmo código -> deve reativar (não falhar)
        var recadastro = await servico.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Arroz Novo", PrecoVendaReais = 27m, Quantidade = 5, CodigoBarras = "7891234567890"
        });

        Assert.True(recadastro.Sucesso);
        Assert.Equal(criado.IdGerado, recadastro.IdGerado); // mesmo registro reativado
        using var context = banco.CreateDbContext();
        var m = context.Mercadorias.Single(x => x.CodigoBarras == "7891234567890");
        Assert.True(m.Ativo);
        Assert.Equal("Arroz Novo", m.Nome);
        Assert.Equal(2700, m.PrecoVenda);
    }

    [Fact]
    public async Task Cadastrar_codigo_de_item_ATIVO_continua_bloqueado()
    {
        using var banco = new BancoDeTeste();
        var servico = CriarMercadoriaService(banco);
        await servico.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Arroz", PrecoVendaReais = 25m, Quantidade = 10, CodigoBarras = "7891234567890"
        });

        var duplicado = await servico.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Cópia", PrecoVendaReais = 25m, Quantidade = 1, CodigoBarras = "7891234567890"
        });

        Assert.False(duplicado.Sucesso);
    }

    // Pendência 4 — overflow do total da venda é barrado, não corrompido
    [Fact]
    public async Task Total_de_venda_acima_do_limite_falha_sem_persistir()
    {
        using var banco = new BancoDeTeste();
        // 1.000.000 unidades x 9.999.999 centavos = 9,99e12 > int.MaxValue
        var m = banco.CriarMercadoria(estoque: 2_000_000, precoVenda: 9_999_999);
        var servico = CriarVendaService(banco);

        var resultado = await servico.FinalizarVendaAsync(null, new[] { new ItemCarrinho(m.Id, 1_000_000) });

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Contains("limite"));
        using var context = banco.CreateDbContext();
        Assert.Equal(0, context.Vendas.Count());               // rollback
        Assert.Equal(2_000_000, banco.EstoqueAtual(m.Id));     // estoque intacto
    }
}
