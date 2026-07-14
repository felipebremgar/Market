using System.Windows;
using Market.Application.Services;
using Market.Domain;

namespace Market.UI.Views;

public partial class ReciboWindow : Window
{
    public ReciboWindow(ReciboVenda recibo, InfoPagamento? pagamento = null)
    {
        InitializeComponent();

        TxtCabecalho.Text = $"Venda #{recibo.VendaId}  ·  {recibo.DataVenda:dd/MM/yyyy HH:mm}";
        TxtCliente.Text = recibo.ClienteNome is null
            ? "Cliente: —"
            : $"Cliente: {recibo.ClienteNome} ({recibo.ClienteCpf})";
        TxtTotal.Text = Moeda.ParaTexto(recibo.TotalCentavos);

        if (pagamento is not null)
        {
            TxtPagamento.Text = pagamento.Forma == FormaPagamento.Dinheiro
                ? $"Pagamento: {pagamento.FormaTexto}  ·  Recebido: {Moeda.ParaTexto(pagamento.ValorPagoCentavos)}  ·  Troco: {Moeda.ParaTexto(pagamento.TrocoCentavos)}"
                : $"Pagamento: {pagamento.FormaTexto}";
            TxtPagamento.Visibility = System.Windows.Visibility.Visible;
        }

        GridItens.ItemsSource = recibo.Itens
            .Select(i => new
            {
                i.Nome,
                i.Quantidade,
                PrecoTexto = Moeda.ParaTexto(i.PrecoUnitarioCentavos),
                SubtotalTexto = Moeda.ParaTexto(i.SubtotalCentavos)
            })
            .ToList();
    }

    private void BtnNovaVenda_Click(object sender, RoutedEventArgs e) => Close();
}
