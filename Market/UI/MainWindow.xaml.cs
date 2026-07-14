using System.Windows;
using Market.Infrastructure.Data;

namespace Market.UI;

public partial class MainWindow : Window
{
    public MainWindow(DatabaseInitializer databaseInitializer)
    {
        InitializeComponent();

        var tables = databaseInitializer.GetTableNames();
        StatusText.Text = $"✔ Banco conectado: {databaseInitializer.DatabasePath}";
        TablesText.Text = $"Tabelas ({tables.Count}): {string.Join(", ", tables)}";
    }
}
