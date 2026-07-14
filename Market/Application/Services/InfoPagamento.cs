namespace Market.Application.Services;

/// <summary>Forma de pagamento escolhida no recebimento.</summary>
public enum FormaPagamento { Dinheiro, Cartao, Pix }

/// <summary>
/// Dados do recebimento de uma venda (forma, valor pago e troco, em centavos).
/// Estado de tela — usado para exibir o troco e imprimir no recibo; não é persistido.
/// </summary>
public record InfoPagamento(FormaPagamento Forma, int ValorPagoCentavos, int TrocoCentavos)
{
    public string FormaTexto => Forma switch
    {
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.Cartao => "Cartão",
        FormaPagamento.Pix => "Pix",
        _ => Forma.ToString()
    };
}
