namespace Market.Application.Services;

/// <summary>Uma linha do carrinho: qual mercadoria e quantas unidades.</summary>
public record ItemCarrinho(int MercadoriaId, int Quantidade);
