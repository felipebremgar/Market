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

    public int Quantidade { get; set; }

    // Valores congelados, em centavos.
    public int PrecoUnitario { get; set; }
    public int PrecoCusto { get; set; }
}
