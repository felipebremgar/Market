using Market.Application.Services;
using Market.Domain;
using Market.UI.Views;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia o recibo numa thread STA em modo histórico (com fiado
/// pendente), garantindo que o XAML e a montagem da linha de pagamento não quebram.
/// </summary>
public class ReciboWindowSmokeTests
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
    public void Recibo_carrega_em_modo_historico()
    {
        OnSta(() =>
        {
            var recibo = new ReciboVenda(
                1, DateTime.Now, "Maria", "52998224725", 250,
                new[] { new ReciboItem("Arroz", 1, 250, UnidadeMedida.Unidade, 250) },
                FormaPagamento.Fiado, StatusPagamento.Pendente,
                DateOnly.FromDateTime(DateTime.Today.AddDays(10)));

            var janela = new ReciboWindow(recibo, historico: true);
            Assert.NotNull(janela);
        });
    }
}
