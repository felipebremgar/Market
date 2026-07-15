namespace Market.Domain;

/// <summary>
/// Cálculo do valor de um item de venda — fonte única da matemática do dinheiro,
/// usada tanto no carrinho quanto ao congelar os valores na venda. Manter isso em um
/// só lugar é o que garante que carrinho, recibo, histórico e relatório sempre batam.
/// </summary>
public static class CalculoItem
{
    /// <summary>Gramas por quilo — itens por peso guardam a quantidade em gramas.</summary>
    public const int GramasPorQuilo = 1000;

    /// <summary>
    /// Valor total do item em centavos.
    /// Por unidade: <c>quantidade × preço</c>.
    /// Por quilo: a quantidade está em GRAMAS e o preço é por KG; arredonda ao centavo
    /// (o valor é congelado na venda, então este arredondamento acontece uma única vez).
    /// Retorna <c>long</c> de propósito: a multiplicação em <c>int</c> estouraria em
    /// silêncio e derrubaria a proteção de limite da venda.
    /// </summary>
    public static long Total(UnidadeMedida unidade, int quantidade, int precoCentavos)
    {
        if (unidade != UnidadeMedida.Quilo)
            return (long)quantidade * precoCentavos;

        return (long)Math.Round(
            quantidade * (decimal)precoCentavos / GramasPorQuilo, MidpointRounding.AwayFromZero);
    }
}
