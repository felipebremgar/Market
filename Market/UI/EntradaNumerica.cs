using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Market.UI;

/// <summary>
/// Filtros de entrada numérica reutilizáveis (digitação + colagem) e parse tolerante,
/// compartilhados entre o cadastro (Dia 3) e o módulo Manter Mercadorias (Dia 4).
/// </summary>
public static class EntradaNumerica
{
    // ----- Filtros de digitação (PreviewTextInput) -----

    public static void FiltrarDecimal(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        foreach (var c in e.Text)
        {
            if (char.IsDigit(c)) continue;
            if (c == ',' && !textBox.Text.Contains(',')) continue;
            e.Handled = true;
            return;
        }
    }

    public static void FiltrarInteiro(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsDigit);

    // ----- Seleção ao focar -----

    /// <summary>
    /// Faz os campos selecionarem todo o conteúdo ao receber foco (teclado ou mouse),
    /// para que a digitação substitua o valor atual — evita o bug do "0" inicial que
    /// ficava na frente do número digitado (ex.: quantidade virava "05").
    /// </summary>
    public static void SelecionarTudoAoFocar(params TextBox[] campos)
    {
        foreach (var textBox in campos)
        {
            textBox.GotKeyboardFocus += (_, _) => textBox.SelectAll();
            textBox.PreviewMouseLeftButtonDown += (_, e) =>
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    e.Handled = true;   // impede o clique de posicionar o cursor
                    textBox.Focus();    // dispara GotKeyboardFocus → SelectAll
                }
            };
        }
    }

    // ----- Filtros de colagem (DataObject.Pasting) -----

    public static void ColarDecimal(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            && e.DataObject.GetData(DataFormats.UnicodeText) is string texto
            && texto.All(c => char.IsDigit(c) || c is ',' or '.'))
            return;
        e.CancelCommand();
    }

    public static void ColarInteiro(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            && e.DataObject.GetData(DataFormats.UnicodeText) is string texto
            && texto.All(char.IsDigit))
            return;
        e.CancelCommand();
    }

    // ----- Parse -----

    // Limite defensivo (R$): impede overflow na conversão para centavos.
    public const decimal MaxReais = 9_999_999m;

    /// <summary>
    /// Faz parse de reais. Vazio = 0 (sucesso). Formato inválido ou acima do máximo
    /// adiciona mensagem a <paramref name="erros"/> e retorna false.
    /// </summary>
    public static bool TryParseReais(string texto, string rotulo, List<string> erros, out decimal reais)
    {
        reais = 0m;
        texto = texto.Trim();
        if (texto.Length == 0) return true;

        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out reais))
        {
            erros.Add($"{rotulo} está em formato inválido.");
            reais = 0m;
            return false;
        }
        if (reais > MaxReais)
        {
            erros.Add($"{rotulo} excede o máximo permitido.");
            reais = 0m;
            return false;
        }
        return true;
    }

    public static bool TryParseInteiro(string texto, string rotulo, List<string> erros, out int valor)
    {
        valor = 0;
        texto = texto.Trim();
        if (texto.Length == 0) return true;

        if (!int.TryParse(texto, NumberStyles.Integer, CultureInfo.CurrentCulture, out valor))
        {
            erros.Add($"{rotulo} está em formato inválido.");
            valor = 0;
            return false;
        }
        return true;
    }

    /// <summary>Parse simples de reais para filtros: vazio ou inválido vira null (ignora o critério).</summary>
    public static decimal? ParseReaisOpcional(string texto)
        => decimal.TryParse(texto?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var v) ? v : null;

    public static int? ParseInteiroOpcional(string texto)
        => int.TryParse(texto?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var v) ? v : null;
}
