using Market.Domain;

namespace Market.Tests.Domain;

public class UnidadeMedidaTests
{
    [Theory]
    [InlineData(UnidadeMedida.Unidade, "Unidade")]
    [InlineData(UnidadeMedida.Quilo, "Kg")]
    public void Texto_amigavel(UnidadeMedida unidade, string esperado)
        => Assert.Equal(esperado, unidade.Texto());

    [Fact]
    public void Sufixo_de_preco_distingue_kg_de_unidade()
    {
        Assert.Equal("por kg", UnidadeMedida.Quilo.SufixoPreco());
        Assert.Equal("por unidade", UnidadeMedida.Unidade.SufixoPreco());
    }

    [Fact]
    public void Formata_quantidade_por_unidade()
        => Assert.Equal("2 un", UnidadeMedida.Unidade.FormatarQuantidade(2));

    // Asserção independente de cultura: o separador decimal muda entre pt-BR e a CI.
    [Fact]
    public void Formata_quantidade_por_peso_convertendo_gramas_em_kg()
    {
        var texto = UnidadeMedida.Quilo.FormatarQuantidade(750);

        Assert.Contains("750", texto);   // 0,750 (pt-BR) ou 0.750
        Assert.EndsWith("kg", texto);
    }
}
