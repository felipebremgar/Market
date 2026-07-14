using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Market.Domain;
using Market.Domain.Repositories;
using Market.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Market.UI.Views;

public partial class HomeView : UserControl
{
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<Mercadoria> _mercadorias;
    private readonly BackupService _backup;
    private readonly ILogger<HomeView> _logger;

    public HomeView(
        DatabaseInitializer databaseInitializer,
        IRepository<Cliente> clientes,
        IRepository<Mercadoria> mercadorias,
        BackupService backup,
        ILogger<HomeView> logger)
    {
        InitializeComponent();
        _databaseInitializer = databaseInitializer;
        _clientes = clientes;
        _mercadorias = mercadorias;
        _backup = backup;
        _logger = logger;
        Loaded += OnLoaded;
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

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var tabelas = _databaseInitializer.GetTableNames();
        StatusText.Text = $"✔ Banco conectado: {_databaseInitializer.DatabasePath}";

        var clientes = await _clientes.GetAllAsync();
        var mercadorias = await _mercadorias.GetAllAsync();

        var texto = new StringBuilder();
        texto.AppendLine($"Tabelas ({tabelas.Count}): {string.Join(", ", tabelas)}");
        texto.AppendLine($"Clientes: {clientes.Count}  |  Mercadorias: {mercadorias.Count}");
        texto.AppendLine();
        foreach (var m in mercadorias)
            texto.AppendLine($"  • {m.Nome} — {Moeda.ParaTexto(m.PrecoVenda)} (estoque {m.Quantidade})");

        TablesText.Text = texto.ToString();
    }
}
