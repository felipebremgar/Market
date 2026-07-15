using Market.Domain;

namespace Market.Tests.Domain;

public class FormaPagamentoTests
{
    [Theory]
    [InlineData(FormaPagamento.Dinheiro, "Dinheiro")]
    [InlineData(FormaPagamento.Cartao, "Cartão")]
    [InlineData(FormaPagamento.Pix, "Pix")]
    [InlineData(FormaPagamento.Fiado, "Fiado")]
    public void Texto_amigavel(FormaPagamento forma, string esperado)
        => Assert.Equal(esperado, forma.Texto());
}
