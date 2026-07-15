using System.Windows;
using Market.UI.Controls;

namespace Market.Tests.UI;

/// <summary>
/// Smoke test de runtime: instancia o NotificationBanner numa thread STA para garantir
/// que o XAML do controle carrega (InitializeComponent) e que a visibilidade alterna
/// corretamente. Valida a peça nova central da v1.3 fora do compilador.
/// </summary>
public class NotificationBannerSmokeTests
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
    public void Banner_carrega_o_xaml_e_alterna_visibilidade()
    {
        OnSta(() =>
        {
            var banner = new NotificationBanner();
            Assert.Equal(Visibility.Collapsed, banner.Visibility); // vazio: não ocupa espaço

            banner.Erro("Falha de teste");
            Assert.Equal(Visibility.Visible, banner.Visibility);

            banner.Sucesso("Ok", autoDismiss: false);
            Assert.Equal(Visibility.Visible, banner.Visibility);

            banner.Limpar();
            Assert.Equal(Visibility.Collapsed, banner.Visibility);
        });
    }
}
