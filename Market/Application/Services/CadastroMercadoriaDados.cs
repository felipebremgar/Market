namespace Market.Application.Services;

/// <summary>
/// Dados de entrada do cadastro de mercadoria, como vêm da tela: preços em reais
/// (decimal) — a conversão para centavos é responsabilidade do serviço.
/// </summary>
public record CadastroMercadoriaDados
{
    public string Nome { get; init; } = string.Empty;
    public string? Fornecedor { get; init; }
    public decimal PrecoCustoReais { get; init; }
    public decimal PrecoVendaReais { get; init; }
    public int Quantidade { get; init; }
    public string? CodigoBarras { get; init; }
    public DateOnly? Validade { get; init; }
}
