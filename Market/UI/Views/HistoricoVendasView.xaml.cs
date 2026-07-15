using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Market.Application.Services;
using Market.Domain;
using Market.UI.Controls;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class HistoricoVendasView : UserControl
{
    private readonly HistoricoService _servico;
    private readonly ILogger<HistoricoVendasView> _logger;

    public HistoricoVendasView(HistoricoService servico, ILogger<HistoricoVendasView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _logger = logger;
        Loaded += async (_, _) => await CarregarAsync(FiltroVenda.Nenhum);
    }

    private async Task CarregarAsync(FiltroVenda filtro)
    {
        try
        {
            var vendas = await _servico.BuscarVendasAsync(filtro);
            GridVendas.ItemsSource = vendas;
            TxtContador.Text = $"{vendas.Count} venda(s)";
            GridItens.ItemsSource = null;
            TxtDetalheCabecalho.Text = "Itens da venda";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar histórico de vendas.");
            MostrarErro("Não foi possível carregar o histórico.");
        }
    }

    private async void GridVendas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridVendas.SelectedItem is not VendaResumo venda)
        {
            GridItens.ItemsSource = null;
            return;
        }

        try
        {
            var itens = await _servico.ObterItensAsync(venda.Id);
            GridItens.ItemsSource = itens.Select(i => new
            {
                i.Nome,
                i.Quantidade,
                PrecoTexto = Moeda.ParaTexto(i.PrecoUnitarioCentavos),
                SubtotalTexto = Moeda.ParaTexto(i.SubtotalCentavos)
            }).ToList();
            TxtDetalheCabecalho.Text = $"Itens da venda #{venda.Id}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar itens da venda {Id}.", venda.Id);
            MostrarErro("Não foi possível carregar os itens da venda.");
        }
    }

    private async void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        => await BotaoOcupado.ExecutarAsync(BtnFiltrar, "Filtrando…", () => CarregarAsync(ConstruirFiltro()));

    private async void Filtro_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; await CarregarAsync(ConstruirFiltro()); }
    }

    private async void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        FiltroDataIni.SelectedDate = null;
        FiltroDataFim.SelectedDate = null;
        FiltroCliente.Clear();
        FiltroProduto.Clear();
        LimparMensagem();
        await CarregarAsync(FiltroVenda.Nenhum);
    }

    private FiltroVenda ConstruirFiltro() => new()
    {
        DataIni = FiltroDataIni.SelectedDate is DateTime di ? DateOnly.FromDateTime(di) : null,
        DataFim = FiltroDataFim.SelectedDate is DateTime df ? DateOnly.FromDateTime(df) : null,
        ClienteCpf = Normalizar(Cpf.Normalizar(FiltroCliente.Text)),
        ProdutoNome = Normalizar(FiltroProduto.Text)
    };

    private static string? Normalizar(string texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private void MostrarErro(string mensagem) => Notificacao.Erro(mensagem);

    private void LimparMensagem() => Notificacao.Limpar();
}
