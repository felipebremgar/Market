using System.Windows;
using Market.Application.Services;
using Market.Domain;

namespace Market.UI.Views;

public partial class ReciboWindow : Window
{
    public ReciboWindow(ReciboVenda recibo, InfoPagamento? pagamento = null, bool historico = false)
    {
        InitializeComponent();

        if (historico)
        {
            Title = $"Recibo — Venda #{recibo.VendaId}";
            TxtTitulo.Text = $"Recibo — Venda #{recibo.VendaId}";
            BtnNovaVenda.Content = "Fechar";
        }

        TxtCabecalho.Text = $"Venda #{recibo.VendaId}  ·  {recibo.DataVenda:dd/MM/yyyy HH:mm}";
        TxtCliente.Text = recibo.ClienteNome is null
            ? "Cliente: —"
            : $"Cliente: {recibo.ClienteNome} ({recibo.ClienteCpf})";
        TxtTotal.Text = Moeda.ParaTexto(recibo.TotalCentavos);

        var textoPagamento = TextoPagamento(recibo, pagamento);
        if (textoPagamento is not null)
        {
            TxtPagamento.Text = textoPagamento;
            TxtPagamento.Visibility = Visibility.Visible;
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

    /// <summary>
    /// Monta a linha de pagamento. A forma vem do recebimento (fluxo do PDV, com troco) ou,
    /// na reabertura pelo histórico, da própria venda persistida.
    /// </summary>
    private static string? TextoPagamento(ReciboVenda recibo, InfoPagamento? pagamento)
    {
        var forma = pagamento?.Forma ?? recibo.Forma;
        if (forma is null) return null;

        return forma switch
        {
            FormaPagamento.Dinheiro when pagamento is not null =>
                $"Pagamento: {forma.Value.Texto()}  ·  Recebido: {Moeda.ParaTexto(pagamento.ValorPagoCentavos)}  ·  Troco: {Moeda.ParaTexto(pagamento.TrocoCentavos)}",
            FormaPagamento.Fiado =>
                $"Pagamento: {forma.Value.Texto()}{TextoFiado(recibo, pagamento)}",
            _ => $"Pagamento: {forma.Value.Texto()}"
        };
    }

    private static string TextoFiado(ReciboVenda recibo, InfoPagamento? pagamento)
    {
        if (recibo.Status == StatusPagamento.Pago) return "  ·  Pago";
        var vencimento = pagamento?.DataVencimento ?? recibo.DataVencimento;
        return vencimento is DateOnly d ? $"  ·  Vence em: {d:dd/MM/yyyy}" : string.Empty;
    }

    private void BtnNovaVenda_Click(object sender, RoutedEventArgs e) => Close();
}
