using Market.Domain;

namespace Market.Tests.Domain;

public class ContatoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Vazio_e_valido(string? valor) => Assert.True(Contato.EhValido(valor));

    [Theory]
    [InlineData("maria@email.com")]
    [InlineData("joao.silva@empresa.com.br")]
    public void Email_valido(string valor) => Assert.True(Contato.EhValido(valor));

    [Theory]
    [InlineData("(92) 99999-8888")]
    [InlineData("9299998888")]
    [InlineData("92 3232-1010")]
    public void Telefone_valido(string valor) => Assert.True(Contato.EhValido(valor));

    [Theory]
    [InlineData("@@@")]
    [InlineData("sem-dominio@")]
    [InlineData("123")]           // poucos dígitos
    [InlineData("abc def")]       // texto sem @ e sem dígitos
    public void Invalido(string valor) => Assert.False(Contato.EhValido(valor));

    [Fact]
    public void Normalizar_apara_espacos_e_vazio_vira_nulo()
    {
        Assert.Equal("x@y.com", Contato.Normalizar("  x@y.com "));
        Assert.Null(Contato.Normalizar("   "));
    }
}
