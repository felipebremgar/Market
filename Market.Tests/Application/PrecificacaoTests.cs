using Market.Application.Services;

namespace Market.Tests.Application;

public class PrecificacaoTests
{
    [Fact]
    public void Preco_sugerido_aplica_a_margem()   // R$100 + 25% => R$125
        => Assert.Equal(12500, Precificacao.PrecoSugeridoCentavos(10000, 25m));

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Preco_sugerido_com_custo_nao_positivo_e_zero(int custo)
        => Assert.Equal(0, Precificacao.PrecoSugeridoCentavos(custo, 25m));

    [Fact]
    public void Preco_sugerido_arredonda_ao_centavo()   // 333 × 1,25 = 416,25 → 416
        => Assert.Equal(416, Precificacao.PrecoSugeridoCentavos(333, 25m));

    [Fact]
    public void Margem_positiva()   // custo R$100, venda R$125 => 25%
        => Assert.Equal(25m, Precificacao.MargemPercent(10000, 12500));

    [Fact]
    public void Margem_negativa_quando_venda_abaixo_do_custo()
        => Assert.True(Precificacao.MargemPercent(10000, 8000) < 0);

    [Fact]
    public void Margem_nula_quando_custo_zero()
        => Assert.Null(Precificacao.MargemPercent(0, 5000));
}
