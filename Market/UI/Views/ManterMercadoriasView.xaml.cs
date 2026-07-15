using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain;
using Market.UI.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class ManterMercadoriasView : UserControl
{
    private readonly MercadoriaService _servico;
    private readonly IServiceProvider _services;
    private readonly ILogger<ManterMercadoriasView> _logger;
    private readonly int _alertaEstoque;

    private FiltroMercadoria _filtroAtual = FiltroMercadoria.Nenhum;

    public ManterMercadoriasView(
        MercadoriaService servico,
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<ManterMercadoriasView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _services = services;
        _logger = logger;
        _alertaEstoque = configuration.GetValue("Estoque:AlertaMinimo", 10);

        Loaded += async (_, _) => await CarregarAsync(FiltroMercadoria.Nenhum);
    }

    private async Task CarregarAsync(FiltroMercadoria filtro)
    {
        _filtroAtual = filtro;
        try
        {
            var mercadorias = await _servico.ListarAsync(filtro);
            Grid.ItemsSource = mercadorias
                .Select(m => new MercadoriaLinha(m, _alertaEstoque))
                .ToList();
            TxtContador.Text = $"{mercadorias.Count} mercadoria(s)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar mercadorias.");
            MostrarErro("Não foi possível carregar as mercadorias.");
        }
    }

    // ----- Edição -----

    private void BtnEditar_Click(object sender, RoutedEventArgs e) => EditarSelecionada();

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => EditarSelecionada();

    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Seleciona a linha sob o cursor para o menu de contexto agir sobre ela.
        var origem = e.OriginalSource as DependencyObject;
        while (origem is not null and not DataGridRow)
            origem = VisualTreeHelper.GetParent(origem);
        if (origem is DataGridRow linha)
            linha.IsSelected = true;
    }

    private void EditarSelecionada()
    {
        if (Grid.SelectedItem is not MercadoriaLinha linha)
        {
            MostrarAviso("Selecione uma mercadoria para editar.");
            return;
        }

        var janela = _services.GetRequiredService<EditarMercadoriaWindow>();
        janela.Owner = Window.GetWindow(this);
        janela.Carregar(linha.Fonte);
        janela.ShowDialog();

        if (janela.Salvou)
            _ = CarregarAsync(_filtroAtual); // recarrega mantendo o filtro atual
    }

    private async void BtnExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not MercadoriaLinha linha)
        {
            MostrarAviso("Selecione uma mercadoria para excluir.");
            return;
        }

        var confirmacao = MessageBox.Show(
            $"Excluir \"{linha.Nome}\"? O item deixa de aparecer nas listagens, mas o histórico é preservado.",
            "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmacao != MessageBoxResult.Yes)
            return;

        await BotaoOcupado.ExecutarAsync(BtnExcluir, "Excluindo…", async () =>
        {
            var resultado = await _servico.ExcluirAsync(linha.Id);
            if (resultado.Sucesso)
            {
                await CarregarAsync(_filtroAtual);
                MostrarAviso($"\"{linha.Nome}\" foi excluída.");
            }
            else
            {
                MostrarErro(resultado.MensagemErro);
            }
        });
    }

    // ----- Filtros -----

    private async void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        => await BotaoOcupado.ExecutarAsync(BtnFiltrar, "Filtrando…", () => CarregarAsync(ConstruirFiltro()));

    private async void BtnLimparFiltros_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        FiltroNome.Clear();
        FiltroFornecedor.Clear();
        FiltroCodigoBarras.Clear();
        FiltroQtdMin.Clear();
        FiltroPrecoMin.Clear();
        FiltroPrecoMax.Clear();
        FiltroValidadeIni.SelectedDate = null;
        FiltroValidadeFim.SelectedDate = null;
        LimparMensagem();
        await CarregarAsync(FiltroMercadoria.Nenhum);
    }

    private async void Filtro_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await CarregarAsync(ConstruirFiltro());
    }

    private FiltroMercadoria ConstruirFiltro()
    {
        var precoMin = EntradaNumerica.ParseReaisOpcional(FiltroPrecoMin.Text);
        var precoMax = EntradaNumerica.ParseReaisOpcional(FiltroPrecoMax.Text);

        return new FiltroMercadoria
        {
            Nome = Normalizar(FiltroNome.Text),
            Fornecedor = Normalizar(FiltroFornecedor.Text),
            CodigoBarras = Normalizar(FiltroCodigoBarras.Text),
            PrecoMinCentavos = precoMin is decimal min ? Moeda.ParaCentavos(min) : null,
            PrecoMaxCentavos = precoMax is decimal max ? Moeda.ParaCentavos(max) : null,
            QtdMin = EntradaNumerica.ParseInteiroOpcional(FiltroQtdMin.Text),
            ValidadeIni = FiltroValidadeIni.SelectedDate is DateTime di ? DateOnly.FromDateTime(di) : null,
            ValidadeFim = FiltroValidadeFim.SelectedDate is DateTime df ? DateOnly.FromDateTime(df) : null
        };
    }

    private static string? Normalizar(string texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // Delegam ao utilitário compartilhado.
    private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarDecimal(sender, e);
    private void Inteiro_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarInteiro(sender, e);
    private void Decimal_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarDecimal(sender, e);
    private void Inteiro_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarInteiro(sender, e);

    // ----- Localizar ao bipar -----

    private async void FiltroCodigoBarras_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        // Consome o Enter (fim de leitura) para não acionar ação padrão.
        e.Handled = true;

        var codigo = FiltroCodigoBarras.Text.Trim();
        if (codigo.Length == 0)
        {
            await CarregarAsync(ConstruirFiltro());
            return;
        }

        await CarregarAsync(ConstruirFiltro());

        // Seleciona a linha encontrada, ou avisa que não existe.
        if (Grid.Items.Count > 0)
        {
            Grid.SelectedIndex = 0;
            Grid.ScrollIntoView(Grid.Items[0]);
            LimparMensagem();
        }
        else
        {
            MostrarAviso($"Nenhuma mercadoria ativa com o código de barras {codigo}.");
        }

        FiltroCodigoBarras.Focus();
        FiltroCodigoBarras.SelectAll();
    }

    private void MostrarErro(string mensagem) => Notificacao.Erro(mensagem);

    private void MostrarAviso(string mensagem) => Notificacao.Aviso(mensagem, autoDismiss: true);

    private void LimparMensagem() => Notificacao.Limpar();
}
