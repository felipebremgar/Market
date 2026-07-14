using System.Windows;
using System.Windows.Controls;
using Market.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Market.UI;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        Navegar<HomeView>();
    }

    private void Navegar<TView>() where TView : UserControl
        => ContentArea.Content = _services.GetRequiredService<TView>();

    private void BtnInicio_Click(object sender, RoutedEventArgs e) => Navegar<HomeView>();

    private void BtnCadastroMercadoria_Click(object sender, RoutedEventArgs e)
        => Navegar<CadastroMercadoriaView>();

    private void BtnManterMercadorias_Click(object sender, RoutedEventArgs e)
        => Navegar<ManterMercadoriasView>();
}
