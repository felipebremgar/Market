using System.Text.RegularExpressions;

namespace Market.Domain;

/// <summary>
/// Contato opcional do cliente: e-mail ou telefone. Vazio é válido (o campo é opcional);
/// se preenchido, precisa parecer um e-mail ou um telefone com 8 a 15 dígitos.
/// </summary>
public static partial class Contato
{
    public static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public static bool EhValido(string? valor)
    {
        var v = Normalizar(valor);
        if (v is null) return true;                 // opcional
        return v.Contains('@') ? EhEmail(v) : EhTelefone(v);
    }

    private static bool EhEmail(string valor) => EmailRegex().IsMatch(valor);

    private static bool EhTelefone(string valor)
    {
        // Só dígitos e caracteres de máscara comuns; 8 a 15 dígitos.
        var somenteMascara = valor.All(c => char.IsDigit(c) || " ()-+".Contains(c));
        var digitos = valor.Count(char.IsDigit);
        return somenteMascara && digitos is >= 8 and <= 15;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
