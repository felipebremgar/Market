using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Market.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class HomeView : UserControl
{
    private readonly BackupService _backup;
    private readonly ILogger<HomeView> _logger;

    public HomeView(BackupService backup, ILogger<HomeView> logger)
    {
        InitializeComponent();
        _backup = backup;
        _logger = logger;
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
