using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class ClientesView : UserControl
{
    private readonly ClienteService _servico;
    private readonly IServiceProvider _services;
    private readonly ILogger<ClientesView> _logger;

    public ClientesView(ClienteService servico, IServiceProvider services, ILogger<ClientesView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _services = services;
        _logger = logger;
        Loaded += async (_, _) => await BuscarAsync();
    }

    private async Task BuscarAsync()
    {
        try
        {
            var clientes = await _servico.BuscarAsync(
                Nulo(BuscaCpf.Text), Nulo(BuscaNome.Text));
            Grid.ItemsSource = clientes;
            TxtContador.Text = $"{clientes.Count} cliente(s)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao buscar clientes.");
            MostrarErro("Não foi possível buscar os clientes.");
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        => await BotaoOcupado.ExecutarAsync(BtnBuscar, "Buscando…", BuscarAsync);

    private async void Busca_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; await BuscarAsync(); }
    }

    private async void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        BuscaCpf.Clear();
        BuscaNome.Clear();
        LimparMensagem();
        await BuscarAsync();
    }

    private void BtnNovo_Click(object sender, RoutedEventArgs e)
    {
        var janela = _services.GetRequiredService<CadastrarClienteWindow>();
        janela.Owner = Window.GetWindow(this);
        janela.ShowDialog();
        if (janela.Salvou)
            _ = BuscarAsync();
    }

    private static string? Nulo(string texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private void MostrarErro(string mensagem) => Notificacao.Erro(mensagem);

    private void LimparMensagem() => Notificacao.Limpar();
}
