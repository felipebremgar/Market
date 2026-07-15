namespace Market.Domain;

/// <summary>Cabeçalho da venda. Cliente é opcional; total em centavos.</summary>
public class Venda
{
    public int Id { get; set; }

    // Data/hora, armazenada como texto ISO-8601 'YYYY-MM-DDTHH:MM:SS'.
    public DateTime DataVenda { get; set; }

    // Total em centavos (soma dos itens).
    public int ValorTotal { get; set; }

    public string? ClienteCpf { get; set; }
    public Cliente? Cliente { get; set; }

    // Forma de pagamento (nula em vendas anteriores à v1.7).
    public FormaPagamento? Forma { get; set; }

    // Fiado (v1.8): situação, prazo de vencimento e data da baixa (pagamento da dívida).
    public StatusPagamento? Status { get; set; }
    public DateOnly? DataVencimento { get; set; }
    public DateTime? DataBaixa { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}
