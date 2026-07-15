using Market.Application.Services;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;
using Market.UI.Views;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia a tela de cadastro de mercadoria numa thread STA,
/// garantindo que o XAML carrega e que o construtor (SelecionarTudoAoFocar, DisplayDateStart,
/// precificação por TextChanged) não quebra.
/// </summary>
public class CadastroMercadoriaViewSmokeTests
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
    public void View_de_cadastro_carrega_sem_erro()
    {
        OnSta(() =>
        {
            using var banco = new BancoDeTeste();
            var repositorio = new MercadoriaRepository(banco);
            var servico = new MercadoriaService(repositorio, NullLogger<MercadoriaService>.Instance);

            var view = new CadastroMercadoriaView(
                servico, repositorio, NullLogger<CadastroMercadoriaView>.Instance);

            Assert.NotNull(view);
        });
    }
}
