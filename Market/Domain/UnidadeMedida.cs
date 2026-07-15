namespace Market.Domain;

/// <summary>
/// Como a mercadoria é vendida. <see cref="Quilo"/> (verduras, frutas) não tem controle de
/// estoque nem validade, e os preços são por quilo.
/// </summary>
public enum UnidadeMedida { Unidade, Quilo }

public static class UnidadeMedidaExtensions
{
    public static string Texto(this UnidadeMedida unidade) => unidade switch
    {
        UnidadeMedida.Unidade => "Unidade",
        UnidadeMedida.Quilo => "Kg",
        _ => unidade.ToString()
    };

    /// <summary>Sufixo do rótulo de preço: "por kg" ou "por unidade".</summary>
    public static string SufixoPreco(this UnidadeMedida unidade)
        => unidade == UnidadeMedida.Quilo ? "por kg" : "por unidade";

    /// <summary>
    /// Formata a quantidade conforme a unidade. Itens por peso guardam GRAMAS
    /// (750 → "0,750 kg"); por unidade, a contagem (2 → "2 un").
    /// </summary>
    public static string FormatarQuantidade(this UnidadeMedida unidade, int quantidade)
        => unidade == UnidadeMedida.Quilo
            ? $"{quantidade / 1000m:0.000} kg"
            : $"{quantidade} un";
}
