namespace Market.Domain;

/// <summary>Produto do estoque. Preços em centavos (INTEGER); validade opcional (data).</summary>
public class Mercadoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Fornecedor { get; set; }

    // Valores monetários em centavos.
    public int PrecoCusto { get; set; }
    public int PrecoVenda { get; set; }

    public int Quantidade { get; set; }
    public string? CodigoBarras { get; set; }

    // Data (sem hora), armazenada como texto 'YYYY-MM-DD'.
    public DateOnly? Validade { get; set; }

    // Preenchida pelo banco na inserção (default do DDL).
    public DateTime DataCadastro { get; set; }

    // Exclusão lógica: itens com vendas nunca são apagados fisicamente.
    public bool Ativo { get; set; } = true;

    public ICollection<ItemVenda> ItensVenda { get; set; } = new List<ItemVenda>();
}
