namespace Market.Application.Services;

/// <summary>
/// Cálculos de precificação por margem de lucro. Valores monetários em centavos.
/// </summary>
public static class Precificacao
{
    /// <summary>
    /// Preço de venda sugerido (centavos) a partir do custo e de uma margem em %:
    /// <c>venda = custo × (1 + margem/100)</c>. Ex.: custo R$100 + 25% → R$125.
    /// Custo não positivo retorna 0.
    /// </summary>
    public static int PrecoSugeridoCentavos(int custoCentavos, decimal margemPercent)
    {
        if (custoCentavos <= 0) return 0;
        var venda = custoCentavos * (1 + margemPercent / 100m);
        return (int)Math.Round(venda, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Margem de lucro (%) a partir de custo e venda: <c>(venda - custo) / custo × 100</c>.
    /// Retorna null quando o custo é zero (margem indefinida).
    /// </summary>
    public static decimal? MargemPercent(int custoCentavos, int vendaCentavos)
    {
        if (custoCentavos <= 0) return null;
        return (vendaCentavos - custoCentavos) / (decimal)custoCentavos * 100m;
    }
}
