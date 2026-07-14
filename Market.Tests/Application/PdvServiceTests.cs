using Market.Application.Services;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;

namespace Market.Tests.Application;

public class PdvServiceTests
{
    private static PdvService CriarServico(BancoDeTeste banco)
        => new(new MercadoriaRepository(banco));

    // T31
    [Fact]
    public async Task Localizar_por_codigo_de_produto_ativo_encontra()
    {
        using var banco = new BancoDeTeste();
        banco.CriarMercadoria(codigoBarras: "7891234567890", nome: "Arroz");

        var achado = await CriarServico(banco).LocalizarPorCodigoAsync("7891234567890");

        Assert.NotNull(achado);
        Assert.Equal("Arroz", achado!.Nome);
    }

    // T32
    [Fact]
    public async Task Codigo_inexistente_nao_encontra()
    {
        using var banco = new BancoDeTeste();
        var achado = await CriarServico(banco).LocalizarPorCodigoAsync("0000000000000");
        Assert.Null(achado);
    }

    // T33
    [Fact]
    public async Task Produto_inativo_nao_e_encontrado_por_codigo()
    {
        using var banco = new BancoDeTeste();
        banco.CriarMercadoria(codigoBarras: "7899999999999", nome: "Inativo", ativo: false);

        var achado = await CriarServico(banco).LocalizarPorCodigoAsync("7899999999999");

        Assert.Null(achado);
    }

    // T34
    [Fact]
    public async Task Busca_por_nome_lista_apenas_ativas()
    {
        using var banco = new BancoDeTeste();
        banco.CriarMercadoria(nome: "Arroz Branco", ativo: true);
        banco.CriarMercadoria(nome: "Arroz Integral", ativo: false);

        var achados = await CriarServico(banco).BuscarPorNomeAsync("Arroz");

        Assert.Single(achados);
        Assert.Equal("Arroz Branco", achados[0].Nome);
    }
}
