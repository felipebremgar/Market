namespace Market.Domain;

/// <summary>Forma de pagamento de uma venda.</summary>
public enum FormaPagamento { Dinheiro, Cartao, Pix, Fiado }

/// <summary>Situação de pagamento de uma venda (fiado nasce Pendente até a baixa).</summary>
public enum StatusPagamento { Pago, Pendente }

public static class FormaPagamentoExtensions
{
    /// <summary>Texto amigável da forma de pagamento (para exibição na UI e recibos).</summary>
    public static string Texto(this FormaPagamento forma) => forma switch
    {
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.Cartao => "Cartão",
        FormaPagamento.Pix => "Pix",
        FormaPagamento.Fiado => "Fiado",
        _ => forma.ToString()
    };
}
