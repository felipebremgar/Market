namespace Market.Domain;

/// <summary>
/// Item de uma venda. Preço de venda e custo são "congelados" no momento da venda
/// (copiados da mercadoria), para que o histórico e o lucro não mudem se o cadastro mudar.
/// </summary>
public class ItemVenda
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public int MercadoriaId { get; set; }
    public Mercadoria Mercadoria { get; set; } = null!;

    /// <summary>Contagem para itens por unidade; GRAMAS para itens por quilo.</summary>
    public int Quantidade { get; set; }

    /// <summary>Unidade congelada — define como interpretar <see cref="Quantidade"/>.</summary>
    public UnidadeMedida Unidade { get; set; }

    // Valores congelados, em centavos (por unidade ou por quilo, conforme Unidade).
    public int PrecoUnitario { get; set; }
    public int PrecoCusto { get; set; }

    // Totais do item já calculados e congelados na venda: recibo, histórico e relatório
    // leem estes valores, garantindo que tudo bata inclusive nos itens por peso.
    public int SubtotalCentavos { get; set; }
    public int CustoCentavos { get; set; }
}
