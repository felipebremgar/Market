using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Market.Application.Services;
using Market.Domain;
using Market.UI;

namespace Market.UI.Views;

/// <summary>
/// Recebimento da venda: escolha da forma de pagamento e, para dinheiro, cálculo do troco ao vivo.
/// Devolve <see cref="Pagamento"/> quando confirmado (DialogResult verdadeiro).
/// </summary>
public partial class RecebimentoWindow : Window
{
    private readonly int _totalCentavos;

    /// <summary>Preenchido ao confirmar. Nulo enquanto a janela não é confirmada.</summary>
    public InfoPagamento? Pagamento { get; private set; }

    public RecebimentoWindow(int totalCentavos, bool permiteFiado = true)
    {
        InitializeComponent();
        _totalCentavos = totalCentavos;
        TxtTotal.Text = Moeda.ParaTexto(totalCentavos);

        OpFiado.IsEnabled = permiteFiado;
        if (!permiteFiado)
            OpFiado.ToolTip = "Selecione um cliente para vender fiado.";
        DtVencimento.DisplayDateStart = DateTime.Today;   // sem vencimento no passado
        DtVencimento.SelectedDate = DateTime.Today.AddDays(30);

        // Seleção padrão definida após InitializeComponent: garante que todos os
        // elementos referenciados por Forma_Changed/AtualizarEstado já existem.
        OpDinheiro.IsChecked = true;

        Loaded += (_, _) =>
        {
            AtualizarEstado();
            TxtValorPago.Focus();
        };
    }

    private bool EhDinheiro => OpDinheiro.IsChecked == true;
    private bool EhFiado => OpFiado.IsChecked == true;

    private void Forma_Changed(object sender, RoutedEventArgs e)
    {
        // Dinheiro: mostra valor recebido/troco. Fiado: mostra vencimento, sem troco.
        PainelValorPago.Visibility = EhDinheiro ? Visibility.Visible : Visibility.Collapsed;
        PainelFiado.Visibility = EhFiado ? Visibility.Visible : Visibility.Collapsed;
        PainelTroco.Visibility = EhFiado ? Visibility.Collapsed : Visibility.Visible;
        AtualizarEstado();
        if (EhDinheiro) TxtValorPago.Focus();
    }

    private void DtVencimento_Changed(object sender, SelectionChangedEventArgs e) => AtualizarEstado();

    /// <summary>Fiado depende de um vencimento válido; as demais formas, do troco.</summary>
    private void AtualizarEstado()
    {
        if (EhFiado)
        {
            BtnConfirmar.IsEnabled = DtVencimento.SelectedDate is DateTime d && d.Date >= DateTime.Today;
            return;
        }
        AtualizarTroco();
    }

    private void BtnValorExato_Click(object sender, RoutedEventArgs e)
    {
        TxtValorPago.Text = Moeda.ParaReais(_totalCentavos).ToString("0.00");
        TxtValorPago.CaretIndex = TxtValorPago.Text.Length;
    }

    private void TxtValorPago_TextChanged(object sender, TextChangedEventArgs e) => AtualizarTroco();

    private void TxtValorPago_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && BtnConfirmar.IsEnabled)
        {
            e.Handled = true;
            BtnConfirmar_Click(sender, e);
        }
    }

    /// <summary>Recalcula troco/estado do botão a partir da forma e do valor recebido.</summary>
    private void AtualizarTroco()
    {
        var pagoCentavos = EhDinheiro ? ValorPagoCentavos() : _totalCentavos;
        var diferenca = pagoCentavos - _totalCentavos; // >= 0 troco; < 0 falta

        if (diferenca >= 0)
        {
            TxtRotuloTroco.Text = "TROCO";
            TxtTroco.Text = Moeda.ParaTexto(diferenca);
            PintarTroco("#E8F5E9", "#2E7D32");
            BtnConfirmar.IsEnabled = true;
        }
        else
        {
            TxtRotuloTroco.Text = "FALTA";
            TxtTroco.Text = Moeda.ParaTexto(-diferenca);
            PintarTroco("#FDECEA", "#C62828");
            BtnConfirmar.IsEnabled = false;
        }
    }

    private void PintarTroco(string fundoHex, string textoHex)
    {
        PainelTroco.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fundoHex));
        var texto = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textoHex));
        TxtRotuloTroco.Foreground = texto;
        TxtTroco.Foreground = texto;
    }

    /// <summary>Valor recebido digitado, em centavos. Campo vazio ou inválido = 0.</summary>
    private int ValorPagoCentavos()
    {
        var reais = EntradaNumerica.ParseReaisOpcional(TxtValorPago.Text);
        if (reais is null or < 0 || reais > EntradaNumerica.MaxReais) return 0;
        return Moeda.ParaCentavos(reais.Value);
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (EhFiado)
        {
            if (DtVencimento.SelectedDate is not DateTime venc || venc.Date < DateTime.Today)
            {
                AtualizarEstado();
                return;
            }
            Pagamento = new InfoPagamento(FormaPagamento.Fiado, 0, 0, DateOnly.FromDateTime(venc));
            DialogResult = true;
            return;
        }

        var pago = EhDinheiro ? ValorPagoCentavos() : _totalCentavos;
        if (pago < _totalCentavos) { AtualizarTroco(); return; } // guarda extra

        var forma = OpCartao.IsChecked == true ? FormaPagamento.Cartao
                  : OpPix.IsChecked == true ? FormaPagamento.Pix
                  : FormaPagamento.Dinheiro;

        Pagamento = new InfoPagamento(forma, pago, pago - _totalCentavos);
        DialogResult = true;
    }

    private void BtnVoltar_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    // ----- Filtros de entrada numérica (reutilizados de EntradaNumerica) -----

    private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarDecimal(sender, e);

    private void Decimal_Pasting(object sender, DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarDecimal(sender, e);
}
