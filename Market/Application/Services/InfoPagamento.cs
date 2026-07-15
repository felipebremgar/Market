using Market.Domain;

namespace Market.Application.Services;

/// <summary>
/// Dados do recebimento de uma venda (forma, valor pago e troco, em centavos).
/// Estado de tela — usado para exibir o troco e imprimir no recibo; não é persistido aqui
/// (a forma de pagamento é gravada na própria venda).
/// </summary>
public record InfoPagamento(FormaPagamento Forma, int ValorPagoCentavos, int TrocoCentavos)
{
    public string FormaTexto => Forma.Texto();
}
