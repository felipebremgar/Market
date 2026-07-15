namespace Market.Domain;

/// <summary>Forma de pagamento de uma venda.</summary>
public enum FormaPagamento { Dinheiro, Cartao, Pix }

public static class FormaPagamentoExtensions
{
    /// <summary>Texto amigável da forma de pagamento (para exibição na UI e recibos).</summary>
    public static string Texto(this FormaPagamento forma) => forma switch
    {
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.Cartao => "Cartão",
        FormaPagamento.Pix => "Pix",
        _ => forma.ToString()
    };
}
