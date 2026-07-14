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
    public string PrecoCustoTexto => Moeda.ParaTexto(Fonte.PrecoCusto);
    public string PrecoVendaTexto => Moeda.ParaTexto(Fonte.PrecoVenda);
    public int Quantidade => Fonte.Quantidade;
    public string? CodigoBarras => Fonte.CodigoBarras;
    public string ValidadeTexto => Fonte.Validade?.ToString("dd/MM/yyyy") ?? "—";
    public string DataCadastroTexto => Fonte.DataCadastro.ToString("dd/MM/yyyy");

    public bool Vencida => Fonte.Validade is { } v && v < Hoje;

    public bool ProximaDoVencimento =>
        Fonte.Validade is { } v && v >= Hoje && v <= Hoje.AddDays(7);

    public bool EstoqueBaixo => Fonte.Quantidade <= _alertaEstoque;
}
