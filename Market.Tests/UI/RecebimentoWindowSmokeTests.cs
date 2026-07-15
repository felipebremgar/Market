using Market.UI.Views;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia a janela de recebimento numa thread STA (com e sem
/// fiado permitido), garantindo que o XAML (radios, painel de fiado, troco) carrega e que
/// o construtor (defaults de vencimento, gating do fiado, seleção inicial) não quebra.
/// </summary>
public class RecebimentoWindowSmokeTests
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
    public void Recebimento_carrega_com_e_sem_fiado()
    {
        OnSta(() =>
        {
            var comFiado = new RecebimentoWindow(1000, permiteFiado: true);
            var semFiado = new RecebimentoWindow(1000, permiteFiado: false);
            Assert.NotNull(comFiado);
            Assert.NotNull(semFiado);
        });
    }
}
