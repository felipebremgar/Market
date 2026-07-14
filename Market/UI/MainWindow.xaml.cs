using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Market.Application;
using Market.Infrastructure.Data;
using Market.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Market.UI;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly DatabaseInitializer _databaseInitializer;

    public MainWindow(IServiceProvider services, DatabaseInitializer databaseInitializer)
    {
        InitializeComponent();
        _services = services;
        _databaseInitializer = databaseInitializer;
        VersaoText.Text = AppInfo.VersaoCurta;
        AtualizarStatusBanco();
        Navegar<HomeView>();
    }

    private void AtualizarStatusBanco()
    {
        var conectado = _databaseInitializer.VerificarConexao();
        StatusBanco.Background = new SolidColorBrush(conectado
            ? Color.FromRgb(0x2E, 0x7D, 0x32)   // verde
            : Color.FromRgb(0xC6, 0x28, 0x28));  // vermelho
        StatusBancoText.Text = conectado ? "Banco conectado" : "Banco indisponível";
    }

    private void Navegar<TView>() where TView : UserControl
        => ContentArea.Content = _services.GetRequiredService<TView>();

    private void BtnInicio_Click(object sender, RoutedEventArgs e) => Navegar<HomeView>();

    private void BtnCadastroMercadoria_Click(object sender, RoutedEventArgs e)
        => Navegar<CadastroMercadoriaView>();

    private void BtnManterMercadorias_Click(object sender, RoutedEventArgs e)
        => Navegar<ManterMercadoriasView>();

    private void BtnClientes_Click(object sender, RoutedEventArgs e)
        => Navegar<ClientesView>();

    private void BtnPdv_Click(object sender, RoutedEventArgs e)
        => Navegar<PdvView>();

    private void BtnHistorico_Click(object sender, RoutedEventArgs e)
        => Navegar<HistoricoVendasView>();

    private void BtnRelatorio_Click(object sender, RoutedEventArgs e)
        => Navegar<RelatorioLucroView>();
}
