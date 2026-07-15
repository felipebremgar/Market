using System.Windows;
using System.Windows.Controls;
using Market.Application.Services;
using Market.UI.Controls;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class RelatorioLucroView : UserControl
{
    private readonly RelatorioService _servico;
    private readonly ILogger<RelatorioLucroView> _logger;

    // Últimos dados gerados — reaproveitados na exportação e no gráfico.
    private DateOnly? _ini;
    private DateOnly? _fim;
    private ResumoLucro _resumo = ResumoLucro.Vazio;
    private IReadOnlyList<LucroPorProduto> _porProduto = System.Array.Empty<LucroPorProduto>();
    private IReadOnlyList<LucroPorDia> _porDia = System.Array.Empty<LucroPorDia>();

    public RelatorioLucroView(RelatorioService servico, ILogger<RelatorioLucroView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _logger = logger;
        Loaded += async (_, _) => await GerarAsync();
    }

    private async Task GerarAsync()
    {
        _ini = DataIni.SelectedDate is DateTime di ? DateOnly.FromDateTime(di) : null;
        _fim = DataFim.SelectedDate is DateTime df ? DateOnly.FromDateTime(df) : null;

        try
        {
            _resumo = await _servico.ResumoAsync(_ini, _fim);
            TxtReceita.Text = _resumo.ReceitaTexto;
            TxtCusto.Text = _resumo.CustoTexto;
            TxtLucro.Text = _resumo.LucroTexto;

            _porProduto = await _servico.PorProdutoAsync(_ini, _fim);
            _porDia = await _servico.PorDiaAsync(_ini, _fim);
            GridProduto.ItemsSource = _porProduto;
            GridDia.ItemsSource = _porDia;

            Grafico.Plotar(_porDia.Select(d => (d.DiaTexto[..5], (double)d.LucroCentavos)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar o relatório de lucros.");
            MessageBox.Show("Não foi possível gerar o relatório.", "Mercadinho",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ----- Exportação -----

    private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
        => Exportar("PDF", "Arquivo PDF (*.pdf)|*.pdf", "relatorio-lucros.pdf",
            () => RelatorioExportador.GerarPdf(_ini, _fim, _resumo, _porProduto));

    private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        => Exportar("Excel", "Planilha Excel (*.xlsx)|*.xlsx", "relatorio-lucros.xlsx",
            () => RelatorioExportador.GerarExcel(_ini, _fim, _resumo, _porProduto, _porDia));

    private void Exportar(string tipo, string filtro, string nomePadrao, Func<byte[]> gerar)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = filtro, FileName = nomePadrao };
        if (dialog.ShowDialog() != true) return;

        try
        {
            System.IO.File.WriteAllBytes(dialog.FileName, gerar());
            MessageBox.Show($"Relatório exportado para:\n{dialog.FileName}",
                $"Exportar {tipo}", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao exportar o relatório em {Tipo}.", tipo);
            MessageBox.Show($"Não foi possível exportar o relatório em {tipo}.", "Mercadinho",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGerar_Click(object sender, RoutedEventArgs e)
        => await BotaoOcupado.ExecutarAsync(BtnGerar, "Gerando…", GerarAsync);

    private async void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        DataIni.SelectedDate = null;
        DataFim.SelectedDate = null;
        await GerarAsync();
    }
}
