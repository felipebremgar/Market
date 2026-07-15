using Market.Domain;

namespace Market.Application.Services;

/// <summary>Recibo de uma venda concluída (dados persistidos, para exibição na tela).</summary>
public record ReciboVenda(
    int VendaId,
    DateTime DataVenda,
    string? ClienteNome,
    string? ClienteCpf,
    int TotalCentavos,
    IReadOnlyList<ReciboItem> Itens,
    FormaPagamento? Forma = null,
    StatusPagamento? Status = null,
    DateOnly? DataVencimento = null);

/// <summary>
/// Linha do recibo, com valores congelados no momento da venda.
/// <paramref name="Quantidade"/> é a contagem (itens por unidade) ou o peso em GRAMAS
/// (itens por quilo); <paramref name="SubtotalCentavos"/> vem pronto do banco.
/// </summary>
public record ReciboItem(
    string Nome, int Quantidade, int PrecoUnitarioCentavos, UnidadeMedida Unidade, int SubtotalCentavos)
{
    public string QuantidadeTexto => Unidade.FormatarQuantidade(Quantidade);

    public string PrecoUnitarioTexto => Unidade == UnidadeMedida.Quilo
        ? $"{Moeda.ParaTexto(PrecoUnitarioCentavos)}/kg"
        : Moeda.ParaTexto(PrecoUnitarioCentavos);

    public string SubtotalTexto => Moeda.ParaTexto(SubtotalCentavos);
}
