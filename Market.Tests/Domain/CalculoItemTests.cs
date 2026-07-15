using Market.Domain;

namespace Market.Tests.Domain;

public class CalculoItemTests
{
    [Fact]
    public void Por_unidade_multiplica_quantidade_pelo_preco()
        => Assert.Equal(5000, CalculoItem.Total(UnidadeMedida.Unidade, 2, 2500));

    [Theory]
    [InlineData(1000, 1990, 1990)]  // 1 kg a R$19,90
    [InlineData(500, 1990, 995)]    // 0,500 kg
    [InlineData(750, 1990, 1493)]   // 0,750 kg = 1492,5 → arredonda para 1493
    [InlineData(250, 1990, 498)]    // 0,250 kg = 497,5 → 498
    [InlineData(1, 1990, 2)]        // 1 grama = 1,99 → 2
    public void Por_quilo_converte_gramas_e_arredonda_ao_centavo(int gramas, int precoKg, long esperado)
        => Assert.Equal(esperado, CalculoItem.Total(UnidadeMedida.Quilo, gramas, precoKg));

    // Retornar long é o que preserva a proteção de limite da venda: em int, isto estouraria
    // em silêncio e a venda passaria batido.
    [Fact]
    public void Nao_estoura_com_valores_acima_do_limite_de_int()
    {
        var total = CalculoItem.Total(UnidadeMedida.Unidade, 100_000, 50_000_000);

        Assert.Equal(5_000_000_000_000L, total);
        Assert.True(total > int.MaxValue);
    }
}
