using System.Windows.Controls;
using Market.Domain;
using Market.UI;

namespace Market.Tests.UI;

public class MascaraCpfTests
{
    private static void OnSta(Action acao)
    {
        Exception? capturada = null;
        var thread = new Thread(() =>
        {
            try { acao(); }
            catch (Exception e) { capturada = e; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (capturada is not null) throw capturada;
    }

    // Comportamento real no controle: aplicar a máscara e escrever no campo formata o texto
    // (é o caminho usado por CarregarParaEdicao/PreencherCpf, que atribuem o CPF cru).
    [Fact]
    public void Aplicar_formata_o_texto_escrito_no_campo()
    {
        OnSta(() =>
        {
            var campo = new TextBox();
            MascaraCpf.Aplicar(campo);

            campo.Text = "52998224725";

            Assert.Equal("529.982.247-25", campo.Text);
            Assert.Equal(14, campo.MaxLength);
        });
    }

    [Fact]
    public void Aplicar_formata_valor_ja_preenchido_antes_da_mascara()
    {
        OnSta(() =>
        {
            var campo = new TextBox { Text = "52998224725" };

            MascaraCpf.Aplicar(campo);

            Assert.Equal("529.982.247-25", campo.Text);
        });
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("5", "5")]
    [InlineData("529", "529")]
    [InlineData("5299", "529.9")]
    [InlineData("529982", "529.982")]
    [InlineData("5299822", "529.982.2")]
    [InlineData("529982247", "529.982.247")]
    [InlineData("5299822472", "529.982.247-2")]
    [InlineData("52998224725", "529.982.247-25")]
    public void Formata_progressivamente_enquanto_digita(string digitos, string esperado)
        => Assert.Equal(esperado, MascaraCpf.Formatar(digitos));

    [Fact]
    public void Ja_formatado_permanece_igual()
        => Assert.Equal("529.982.247-25", MascaraCpf.Formatar("529.982.247-25"));

    [Fact]
    public void Descarta_nao_digitos()
        => Assert.Equal("123.456", MascaraCpf.Formatar("abc123def456"));

    [Fact]
    public void Corta_o_excedente_de_11_digitos()
        => Assert.Equal("529.982.247-25", MascaraCpf.Formatar("52998224725999"));

    [Fact]
    public void Nulo_vira_vazio()
        => Assert.Equal("", MascaraCpf.Formatar(null));

    // A máscara não pode atrapalhar os serviços: Cpf.Normalizar remove a formatação.
    [Fact]
    public void Valor_mascarado_normaliza_de_volta_para_11_digitos()
    {
        var mascarado = MascaraCpf.Formatar("52998224725");

        Assert.Equal("52998224725", Cpf.Normalizar(mascarado));
        Assert.True(Cpf.EhValido(mascarado));
    }
}
