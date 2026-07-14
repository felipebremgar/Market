using System.Text;
using System.Windows.Controls;
using Market.Domain;
using Market.Domain.Repositories;
using Market.Infrastructure.Data;

namespace Market.UI.Views;

public partial class HomeView : UserControl
{
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<Mercadoria> _mercadorias;

    public HomeView(
        DatabaseInitializer databaseInitializer,
        IRepository<Cliente> clientes,
        IRepository<Mercadoria> mercadorias)
    {
        InitializeComponent();
        _databaseInitializer = databaseInitializer;
        _clientes = clientes;
        _mercadorias = mercadorias;
        Loaded += OnLoaded;
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
