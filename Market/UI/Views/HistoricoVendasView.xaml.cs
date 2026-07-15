using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain;
using Market.UI.Controls;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class HistoricoVendasView : UserControl
{
    private readonly HistoricoService _servico;
    private readonly FiadoService _fiado;
    private readonly VendaService _vendas;
    private readonly ILogger<HistoricoVendasView> _logger;

    private FiltroVenda _filtroAtual = FiltroVenda.Nenhum;

    public HistoricoVendasView(
        HistoricoService servico, FiadoService fiado, VendaService vendas, ILogger<HistoricoVendasView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _fiado = fiado;
        _vendas = vendas;
        _logger = logger;
        Loaded += async (_, _) => await CarregarAsync(FiltroVenda.Nenhum);
    }

    private async Task CarregarAsync(FiltroVenda filtro)
    {
        _filtroAtual = filtro;
        try
        {
            var vendas = await _servico.BuscarVendasAsync(filtro);
            GridVendas.ItemsSource = vendas;
            var totalCentavos = vendas.Sum(v => (long)v.ValorTotal);
            TxtContador.Text = $"{vendas.Count} venda(s)  ·  Total do período: {Moeda.ParaTexto(totalCentavos)}";
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

    // ----- Recibo -----

    private void GridVendas_DoubleClick(object sender, MouseButtonEventArgs e) => _ = AbrirReciboAsync();

    private void BtnRecibo_Click(object sender, RoutedEventArgs e) => _ = AbrirReciboAsync();

    private async Task AbrirReciboAsync()
    {
        if (GridVendas.SelectedItem is not VendaResumo venda)
        {
            Notificacao.Aviso("Selecione uma venda para ver o recibo.", autoDismiss: true);
            return;
        }

        try
        {
            var recibo = await _vendas.ObterReciboAsync(venda.Id);
            if (recibo is null) { Notificacao.Erro("Recibo não encontrado."); return; }
            new ReciboWindow(recibo, historico: true) { Owner = Window.GetWindow(this) }.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao abrir o recibo da venda {Id}.", venda.Id);
            Notificacao.Erro("Não foi possível abrir o recibo.");
        }
    }

    // ----- Baixa de fiado -----

    private async void BtnBaixa_Click(object sender, RoutedEventArgs e)
    {
        if (GridVendas.SelectedItem is not VendaResumo venda)
        {
            Notificacao.Aviso("Selecione uma venda fiada pendente para dar baixa.", autoDismiss: true);
            return;
        }
        if (!venda.PodeReceberBaixa)
        {
            Notificacao.Aviso("Esta venda não é um fiado pendente.", autoDismiss: true);
            return;
        }

        var confirmar = MessageBox.Show(
            $"Confirmar o pagamento da venda #{venda.Id} ({venda.TotalTexto}) de {venda.ClienteTexto}?\n\nA baixa será registrada com a data de hoje.",
            "Dar baixa no fiado", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes) return;

        await BotaoOcupado.ExecutarAsync(BtnBaixa, "Processando…", async () =>
        {
            var resultado = await _fiado.DarBaixaAsync(venda.Id);
            if (resultado.Sucesso)
            {
                await CarregarAsync(_filtroAtual);
                Notificacao.Sucesso($"Baixa registrada na venda #{venda.Id}.", autoDismiss: true);
            }
            else
            {
                Notificacao.Erro(resultado.MensagemErro);
            }
        });
    }

    private void GridVendas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var origem = e.OriginalSource as DependencyObject;
        while (origem is not null and not DataGridRow)
            origem = VisualTreeHelper.GetParent(origem);
        if (origem is DataGridRow linha)
            linha.IsSelected = true;
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
