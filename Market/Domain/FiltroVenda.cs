namespace Market.Domain;

/// <summary>
/// Critérios de busca do histórico de vendas. Todos opcionais (nulo é ignorado).
/// O período é por dia (inclusive nas duas pontas).
/// </summary>
public record FiltroVenda
{
    public DateOnly? DataIni { get; init; }
    public DateOnly? DataFim { get; init; }
    public string? ClienteCpf { get; init; }

    /// <summary>Filtra vendas que contenham um item cujo produto casa com este nome (parcial).</summary>
    public string? ProdutoNome { get; init; }

    public static FiltroVenda Nenhum { get; } = new();
}
