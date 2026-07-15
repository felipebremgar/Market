using System.Windows;
using Market.UI.Views;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia a janela de peso numa thread STA, garantindo que o XAML
/// carrega e que o construtor (total ao vivo, estado do botão) não quebra.
/// </summary>
public class PesoWindowSmokeTests
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

    [Fact]
    public void Janela_de_peso_carrega_sem_erro_e_nasce_sem_peso()
    {
        OnSta(() =>
        {
            var janela = new PesoWindow("Tomate", 1990);

            Assert.NotNull(janela);
            Assert.Null(janela.Gramas);   // só é preenchido ao confirmar
        });
    }

    /// <summary>
    /// Regressão: com altura fixa, a barra de título/bordas comiam o espaço e os botões
    /// ficavam cortados. SizeToContent garante que a janela sempre comporte o conteúdo,
    /// inclusive com fonte ou escala de tela maiores.
    /// </summary>
    [Fact]
    public void Janela_de_peso_dimensiona_a_altura_pelo_conteudo()
    {
        OnSta(() =>
        {
            var janela = new PesoWindow("Tomate", 1990);

            Assert.Equal(SizeToContent.Height, janela.SizeToContent);
            Assert.True(double.IsNaN(janela.Height), "A altura não pode ser fixa.");
        });
    }
}
