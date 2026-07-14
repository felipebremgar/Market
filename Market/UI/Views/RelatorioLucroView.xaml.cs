using System.Windows;
using System.Windows.Controls;
using Market.Application.Services;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class RelatorioLucroView : UserControl
{
    private readonly RelatorioService _servico;
    private readonly ILogger<RelatorioLucroView> _logger;

    public RelatorioLucroView(RelatorioService servico, ILogger<RelatorioLucroView> logger)
    {
        InitializeComponent();
        _servico = servico;
        _logger = logger;
        Loaded += async (_, _) => await GerarAsync();
    }

    private async Task GerarAsync()
    {
        var ini = DataIni.SelectedDate is DateTime di ? DateOnly.FromDateTime(di) : (DateOnly?)null;
        var fim = DataFim.SelectedDate is DateTime df ? DateOnly.FromDateTime(df) : (DateOnly?)null;

        try
        {
            var resumo = await _servico.ResumoAsync(ini, fim);
            TxtReceita.Text = resumo.ReceitaTexto;
            TxtCusto.Text = resumo.CustoTexto;
            TxtLucro.Text = resumo.LucroTexto;

            GridProduto.ItemsSource = await _servico.PorProdutoAsync(ini, fim);
            GridDia.ItemsSource = await _servico.PorDiaAsync(ini, fim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar o relatório de lucros.");
            MessageBox.Show("Não foi possível gerar o relatório.", "Mercadinho",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGerar_Click(object sender, RoutedEventArgs e) => await GerarAsync();

    private async void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        DataIni.SelectedDate = null;
        DataFim.SelectedDate = null;
        await GerarAsync();
    }
}
