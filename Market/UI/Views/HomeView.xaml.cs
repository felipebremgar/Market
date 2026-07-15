using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Market.Application.Services;
using Market.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class HomeView : UserControl
{
    private readonly BackupService _backup;
    private readonly FiadoService _fiado;
    private readonly ILogger<HomeView> _logger;

    public HomeView(BackupService backup, FiadoService fiado, ILogger<HomeView> logger)
    {
        InitializeComponent();
        _backup = backup;
        _fiado = fiado;
        _logger = logger;
        Loaded += async (_, _) => await CarregarFiadosAsync();
    }

    private async Task CarregarFiadosAsync()
    {
        try
        {
            var fiados = await _fiado.ListarPendentesAsync();
            GridFiados.ItemsSource = fiados;

            var vencidos = fiados.Count(f => f.Vencido);
            var venceSemana = fiados.Count(f => !f.Vencido && f.DiasParaVencer <= 7);

            if (vencidos > 0)
                Notificacao.Erro($"{vencidos} venda(s) fiada(s) vencida(s) aguardando pagamento.");
            else if (venceSemana > 0)
                Notificacao.Aviso($"{venceSemana} venda(s) fiada(s) vencem nos próximos 7 dias.");
            else
                Notificacao.Limpar();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar as vendas fiadas pendentes.");
        }
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var caminho = _backup.RealizarBackup();
            TxtBackup.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
            TxtBackup.Text = $"✔ Backup criado em: {caminho}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao fazer backup do banco.");
            TxtBackup.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
            TxtBackup.Text = "Não foi possível fazer o backup.";
        }
    }
}
