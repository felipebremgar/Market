using Market.Domain;

namespace Market.Application.Services;

/// <summary>
/// Dados de entrada do cadastro de mercadoria, como vêm da tela: preços em reais
/// (decimal) — a conversão para centavos é responsabilidade do serviço.
/// Quando <see cref="Unidade"/> é Quilo, os preços são por kg e o serviço ignora
/// quantidade e validade (esses itens não têm acompanhamento).
/// </summary>
public record CadastroMercadoriaDados
{
    public string Nome { get; init; } = string.Empty;
    public string? Fornecedor { get; init; }
    public UnidadeMedida Unidade { get; init; } = UnidadeMedida.Unidade;
    public decimal PrecoCustoReais { get; init; }
    public decimal PrecoVendaReais { get; init; }
    public int Quantidade { get; init; }
    public string? CodigoBarras { get; init; }
    public DateOnly? Validade { get; init; }
}
