using Market.Domain;

namespace Market.Tests.Domain;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("11144477735")]
    public void CPFs_validos_sao_aceitos(string cpf)
        => Assert.True(Cpf.EhValido(cpf));

    [Fact]
    public void Digito_verificador_errado_e_invalido()
        => Assert.False(Cpf.EhValido("52998224724"));

    [Theory]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("99999999999")]
    public void Todos_os_digitos_iguais_e_invalido(string cpf)
        => Assert.False(Cpf.EhValido(cpf));

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("5299822472X")]
    [InlineData("529982247250")] // 12 dígitos
    public void Entradas_malformadas_sao_invalidas(string? cpf)
        => Assert.False(Cpf.EhValido(cpf));

    [Fact]
    public void CPF_formatado_e_normalizado_e_validado()
    {
        Assert.True(Cpf.EhValido("529.982.247-25"));
        Assert.Equal("52998224725", Cpf.Normalizar("529.982.247-25"));
    }
}
