using Market.Application.Services;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;
using Market.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia o PDV numa thread STA, garantindo que o XAML (painel do
/// cliente, dropdown de busca) carrega e que o construtor (máscara de CPF, timer de busca)
/// não quebra.
/// </summary>
public class PdvViewSmokeTests
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
    public void Pdv_carrega_sem_erro_e_nasce_sem_cliente()
    {
        OnSta(() =>
        {
            using var banco = new BancoDeTeste();
            var pdv = new PdvService(new MercadoriaRepository(banco));
            var clientes = new ClienteService(new ClienteRepository(banco), NullLogger<ClienteService>.Instance);
            var vendas = new VendaService(banco, NullLogger<VendaService>.Instance);
            var services = new ServiceCollection().BuildServiceProvider();

            var view = new PdvView(pdv, clientes, vendas, services, NullLogger<PdvView>.Instance);

            Assert.NotNull(view);
            Assert.Null(view.ClienteCpfSelecionado);   // venda começa sem cliente
        });
    }
}
