namespace Market.Domain;

/// <summary>
/// Critérios de busca de mercadorias. Todos opcionais: campo nulo é ignorado
/// (mesma semântica do WHERE dinâmico "(@x IS NULL OR ...)"). Preços em centavos.
/// </summary>
public record FiltroMercadoria
{
    public string? Nome { get; init; }
    public string? Fornecedor { get; init; }
    public string? CodigoBarras { get; init; }
    public int? PrecoMinCentavos { get; init; }
    public int? PrecoMaxCentavos { get; init; }
    public int? QtdMin { get; init; }
    public DateOnly? ValidadeIni { get; init; }
    public DateOnly? ValidadeFim { get; init; }

    /// <summary>Filtro vazio: retorna todas as mercadorias ativas.</summary>
    public static FiltroMercadoria Nenhum { get; } = new();
}
