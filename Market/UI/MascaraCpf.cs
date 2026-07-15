using System.Windows;
using System.Windows.Controls;

namespace Market.UI;

/// <summary>
/// Máscara de CPF (000.000.000-00): aceita apenas dígitos e formata enquanto o usuário
/// digita, preservando a posição do cursor. Como <see cref="Domain.Cpf.Normalizar"/> remove
/// a máscara, os serviços continuam recebendo o CPF normalizado sem nenhuma mudança.
/// </summary>
public static class MascaraCpf
{
    private const int MaxDigitos = 11;
    private const int MaxComMascara = 14;   // 11 dígitos + "." "." "-"

    /// <summary>
    /// Formata os dígitos no padrão 000.000.000-00, parcialmente enquanto incompleto.
    /// Ignora o que não for dígito e descarta o excedente além de 11.
    /// </summary>
    public static string Formatar(string? valor)
    {
        var d = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        if (d.Length > MaxDigitos) d = d[..MaxDigitos];

        return d.Length switch
        {
            <= 3 => d,
            <= 6 => $"{d[..3]}.{d[3..]}",
            <= 9 => $"{d[..3]}.{d[3..6]}.{d[6..]}",
            _ => $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}"
        };
    }

    /// <summary>Aplica a máscara aos campos: digitação, colagem e formatação automática.</summary>
    public static void Aplicar(params TextBox[] campos)
    {
        foreach (var campo in campos)
        {
            campo.MaxLength = MaxComMascara;
            campo.PreviewTextInput += (_, e) => e.Handled = !e.Text.All(char.IsDigit);
            DataObject.AddPastingHandler(campo, ColarSomenteDigitos);
            campo.TextChanged += (s, _) => Reformatar((TextBox)s);
            Reformatar(campo);   // formata um valor já preenchido (ex.: edição de cliente)
        }
    }

    private static void ColarSomenteDigitos(object sender, DataObjectPastingEventArgs e)
    {
        // Deixa passar se houver algum dígito; o Reformatar descarta o resto.
        if (e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            && e.DataObject.GetData(DataFormats.UnicodeText) is string texto
            && texto.Any(char.IsDigit))
            return;

        e.CancelCommand();
    }

    private static void Reformatar(TextBox campo)
    {
        var formatado = Formatar(campo.Text);
        if (formatado == campo.Text) return;   // já formatado: encerra a recursão do TextChanged

        var digitosAntesDoCursor = campo.Text.Take(campo.CaretIndex).Count(char.IsDigit);
        campo.Text = formatado;
        campo.CaretIndex = PosicaoApos(formatado, digitosAntesDoCursor);
    }

    /// <summary>Posição do cursor logo após a N-ésima casa numérica do texto formatado.</summary>
    private static int PosicaoApos(string texto, int quantidadeDeDigitos)
    {
        if (quantidadeDeDigitos <= 0) return 0;

        var contados = 0;
        for (var i = 0; i < texto.Length; i++)
            if (char.IsDigit(texto[i]) && ++contados == quantidadeDeDigitos)
                return i + 1;

        return texto.Length;
    }
}
