namespace Market.Domain;

/// <summary>
/// Conversão entre reais (entrada/saída do usuário) e centavos (armazenamento).
/// Todo valor monetário no banco é INTEGER em centavos; use este utilitário em toda a aplicação.
/// </summary>
public static class Moeda
{
    // Lê o que o usuário digitou (ex: 9,90) e converte para centavos.
    public static int ParaCentavos(decimal reais) => (int)Math.Round(reais * 100m);

    // Converte centavos do banco para exibição (ex: "R$ 9,90").
    public static string ParaTexto(int centavos) => (centavos / 100m).ToString("C");

    public static decimal ParaReais(int centavos) => centavos / 100m;
}
