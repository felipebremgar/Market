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
    private readonly List<Button> _navButtons;

    public MainWindow(IServiceProvider services, DatabaseInitializer databaseInitializer)
    {
        InitializeComponent();
        _services = services;
        _databaseInitializer = databaseInitializer;
        _navButtons = new List<Button>
        {
            BtnInicio, BtnCadastroMercadoria, BtnManterMercadorias,
            BtnClientes, BtnPdv, BtnHistorico, BtnRelatorio
        };
        VersaoText.Text = AppInfo.VersaoCurta;
        AtualizarStatusBanco();
        Navegar<HomeView>(BtnInicio);
    }

    private void AtualizarStatusBanco()
    {
        var conectado = _databaseInitializer.VerificarConexao();
        StatusBanco.Background = new SolidColorBrush(conectado
            ? Color.FromRgb(0x2E, 0x7D, 0x32)   // verde
            : Color.FromRgb(0xC6, 0x28, 0x28));  // vermelho
        StatusBancoText.Text = conectado ? "Banco conectado" : "Banco indisponível";
    }

    private void Navegar<TView>(Button ativo) where TView : UserControl
    {
        ContentArea.Content = _services.GetRequiredService<TView>();
        // Marca o botão da tela atual (o template destaca quem tiver Tag="ativa").
        foreach (var botao in _navButtons)
            botao.Tag = ReferenceEquals(botao, ativo) ? "ativa" : null;
    }

    private void BtnInicio_Click(object sender, RoutedEventArgs e) => Navegar<HomeView>(BtnInicio);

    private void BtnCadastroMercadoria_Click(object sender, RoutedEventArgs e)
        => Navegar<CadastroMercadoriaView>(BtnCadastroMercadoria);

    private void BtnManterMercadorias_Click(object sender, RoutedEventArgs e)
        => Navegar<ManterMercadoriasView>(BtnManterMercadorias);

    private void BtnClientes_Click(object sender, RoutedEventArgs e)
        => Navegar<ClientesView>(BtnClientes);

    private void BtnPdv_Click(object sender, RoutedEventArgs e)
        => Navegar<PdvView>(BtnPdv);

    private void BtnHistorico_Click(object sender, RoutedEventArgs e)
        => Navegar<HistoricoVendasView>(BtnHistorico);

    private void BtnRelatorio_Click(object sender, RoutedEventArgs e)
        => Navegar<RelatorioLucroView>(BtnRelatorio);
}
