namespace Market.Domain;

/// <summary>
/// Validação e normalização de CPF (dígitos verificadores).
/// </summary>
public static class Cpf
{
    /// <summary>Remove tudo que não for dígito (pontos, traço, espaços).</summary>
    public static string Normalizar(string? cpf)
        => new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>
    /// Valida um CPF: 11 dígitos, não todos iguais, e os dois dígitos verificadores corretos.
    /// Aceita entrada formatada (normaliza antes).
    /// </summary>
    public static bool EhValido(string? cpf)
    {
        var digitos = Normalizar(cpf);
        if (digitos.Length != 11)
            return false;

        // Rejeita sequências de dígito único (00000000000, 11111111111, ...).
        if (digitos.All(c => c == digitos[0]))
            return false;

        var numeros = digitos.Select(c => c - '0').ToArray();

        return numeros[9] == CalcularDigito(numeros, 9)
            && numeros[10] == CalcularDigito(numeros, 10);
    }

    // Dígito verificador na posição informada (9 = 1º DV, 10 = 2º DV),
    // pelo algoritmo padrão da Receita (peso decrescente, módulo 11).
    private static int CalcularDigito(int[] numeros, int posicao)
    {
        var soma = 0;
        var peso = posicao + 1;
        for (var i = 0; i < posicao; i++)
            soma += numeros[i] * peso--;

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
