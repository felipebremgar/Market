using Market.UI.Controls;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia o gráfico de barras numa thread STA e plota dados
/// (positivos e negativos), garantindo que o XAML e o desenho não quebram.
/// </summary>
public class GraficoBarrasSmokeTests
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
    public void Grafico_instancia_e_plota_sem_erro()
    {
        OnSta(() =>
        {
            var grafico = new GraficoBarras();
            grafico.Plotar(new[] { ("10/07", 500.0), ("11/07", -200.0), ("12/07", 0.0) });
            Assert.NotNull(grafico);
        });
    }
}
