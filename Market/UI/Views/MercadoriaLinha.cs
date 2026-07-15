using Market.Domain;

namespace Market.UI.Views;

/// <summary>
/// Adaptador de exibição de uma <see cref="Mercadoria"/> no DataGrid: formata valores
/// e expõe as flags de destaque (vencida, próxima do vencimento, estoque baixo).
/// </summary>
public class MercadoriaLinha
{
    // Calculada a cada acesso: um app aberto por dias mantém os destaques de validade corretos.
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    private readonly int _alertaEstoque;

    public MercadoriaLinha(Mercadoria fonte, int alertaEstoque)
    {
        Fonte = fonte;
        _alertaEstoque = alertaEstoque;
    }

    public Mercadoria Fonte { get; }

    public int Id => Fonte.Id;
    public string Nome => Fonte.Nome;
    public string? Fornecedor => Fonte.Fornecedor;

    /// <summary>Item vendido por peso: preços por kg, sem estoque nem validade.</summary>
    public bool PorPeso => Fonte.Unidade == UnidadeMedida.Quilo;
    public string UnidadeTexto => Fonte.Unidade.Texto();

    public string PrecoCustoTexto => FormatarPreco(Fonte.PrecoCusto);
    public string PrecoVendaTexto => FormatarPreco(Fonte.PrecoVenda);
    public int Quantidade => Fonte.Quantidade;

    /// <summary>Itens por peso não têm acompanhamento de estoque.</summary>
    public string QuantidadeTexto => PorPeso ? "—" : Fonte.Quantidade.ToString();

    public string? CodigoBarras => Fonte.CodigoBarras;
    public string ValidadeTexto => Fonte.Validade?.ToString("dd/MM/yyyy") ?? "—";
    public string DataCadastroTexto => Fonte.DataCadastro.ToString("dd/MM/yyyy");

    public bool Vencida => Fonte.Validade is { } v && v < Hoje;

    public bool ProximaDoVencimento =>
        Fonte.Validade is { } v && v >= Hoje && v <= Hoje.AddDays(7);

    // Itens por peso têm quantidade sempre 0: sem esta guarda, todos seriam
    // marcados como estoque baixo.
    public bool EstoqueBaixo => !PorPeso && Fonte.Quantidade <= _alertaEstoque;

    private string FormatarPreco(int centavos)
        => PorPeso ? $"{Moeda.ParaTexto(centavos)}/kg" : Moeda.ParaTexto(centavos);
}
