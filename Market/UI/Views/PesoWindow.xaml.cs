using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Market.Domain;

namespace Market.UI.Views;

/// <summary>
/// Pede o peso de um item vendido por quilo, mostrando o total ao vivo.
/// Devolve o peso em GRAMAS (inteiro exato) — é assim que o carrinho e a venda o guardam.
/// </summary>
public partial class PesoWindow : Window
{
    private readonly int _precoPorKgCentavos;

    /// <summary>Peso confirmado em gramas. Nulo enquanto a janela não é confirmada.</summary>
    public int? Gramas { get; private set; }

    public PesoWindow(string nome, int precoPorKgCentavos)
    {
        InitializeComponent();
        _precoPorKgCentavos = precoPorKgCentavos;

        TxtProduto.Text = nome;
        TxtPreco.Text = $"{Moeda.ParaTexto(precoPorKgCentavos)} por kg";

        AtualizarTotal();
        Loaded += (_, _) => TxtPeso.Focus();
    }

    /// <summary>Peso digitado (em kg) convertido para gramas. Vazio ou inválido = 0.</summary>
    private int GramasInformados()
    {
        var quilos = EntradaNumerica.ParseReaisOpcional(TxtPeso.Text);
        if (quilos is null or <= 0) return 0;

        return (int)Math.Round(quilos.Value * CalculoItem.GramasPorQuilo, MidpointRounding.AwayFromZero);
    }

    private void TxtPeso_TextChanged(object sender, TextChangedEventArgs e) => AtualizarTotal();

    private void AtualizarTotal()
    {
        var gramas = GramasInformados();
        TxtTotal.Text = Moeda.ParaTexto(CalculoItem.Total(UnidadeMedida.Quilo, gramas, _precoPorKgCentavos));
        BtnConfirmar.IsEnabled = gramas > 0;
    }

    private void TxtPeso_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && BtnConfirmar.IsEnabled)
        {
            e.Handled = true;
            BtnConfirmar_Click(sender, e);
        }
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        var gramas = GramasInformados();
        if (gramas <= 0) return;

        Gramas = gramas;
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => EntradaNumerica.FiltrarDecimal(sender, e);

    private void Decimal_Pasting(object sender, DataObjectPastingEventArgs e)
        => EntradaNumerica.ColarDecimal(sender, e);
}
