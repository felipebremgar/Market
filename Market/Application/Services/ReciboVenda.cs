namespace Market.Application.Services;

/// <summary>Recibo de uma venda concluída (dados persistidos, para exibição na tela).</summary>
public record ReciboVenda(
    int VendaId,
    DateTime DataVenda,
    string? ClienteNome,
    string? ClienteCpf,
    int TotalCentavos,
    IReadOnlyList<ReciboItem> Itens);

/// <summary>Linha do recibo, com valores congelados no momento da venda.</summary>
public record ReciboItem(string Nome, int Quantidade, int PrecoUnitarioCentavos)
{
    public int SubtotalCentavos => Quantidade * PrecoUnitarioCentavos;
}
