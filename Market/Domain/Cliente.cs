namespace Market.Domain;

/// <summary>Cliente do mercadinho. O CPF (11 dígitos) é a chave primária.</summary>
public class Cliente
{
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;

    // Vendas associadas a este cliente (opcional em cada venda).
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
