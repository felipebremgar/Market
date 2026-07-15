using Market.Domain;

namespace Market.Application.Services;

/// <summary>Uma linha do carrinho (estado de exibição do PDV).</summary>
public class LinhaCarrinho
{
    public int MercadoriaId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public UnidadeMedida Unidade { get; init; }
    public int PrecoUnitarioCentavos { get; init; }

    /// <summary>Contagem para itens por unidade; GRAMAS para itens por quilo.</summary>
    public int Quantidade { get; internal set; }
    public int EstoqueDisponivel { get; internal set; }

    /// <summary>Item vendido por peso: sem estoque e sem ajuste por +/−.</summary>
    public bool PorPeso => Unidade == UnidadeMedida.Quilo;
    public bool PodeAjustarQuantidade => !PorPeso;

    // Cast para int mantém o comportamento do carrinho (limitado por estoque/UI); o
    // limite real da venda é validado no VendaService, que trabalha em long.
    public int SubtotalCentavos => (int)CalculoItem.Total(Unidade, Quantidade, PrecoUnitarioCentavos);
    public string QuantidadeTexto => Unidade.FormatarQuantidade(Quantidade);

    /// <summary>Unidade da coluna Qtd no carrinho: gramas para peso, unidades para o resto.</summary>
    public string UnidadeQuantidadeTexto => PorPeso ? "g" : "un";
    public string PrecoUnitarioTexto =>
        PorPeso ? $"{Moeda.ParaTexto(PrecoUnitarioCentavos)}/kg" : Moeda.ParaTexto(PrecoUnitarioCentavos);
    public string SubtotalTexto => Moeda.ParaTexto(SubtotalCentavos);
}

/// <summary>
/// Estado do carrinho de venda, em memória. Consolida linhas por mercadoria, calcula
/// subtotais/total em centavos e barra quantidades acima do estoque disponível (UX —
/// a consistência final é garantida pela transação do <see cref="VendaService"/>).
/// </summary>
public class Carrinho
{
    private readonly List<LinhaCarrinho> _linhas = new();

    public IReadOnlyList<LinhaCarrinho> Linhas => _linhas;
    public bool Vazio => _linhas.Count == 0;
    public int TotalCentavos => _linhas.Sum(l => l.SubtotalCentavos);
    public string TotalTexto => Moeda.ParaTexto(TotalCentavos);

    /// <summary>
    /// Adiciona ao carrinho e consolida se o produto já estiver lá. <paramref name="quantidade"/>
    /// é a contagem para itens por unidade e o PESO EM GRAMAS para itens por quilo (que,
    /// por não terem acompanhamento de estoque, não passam pela validação de disponibilidade).
    /// </summary>
    public ResultadoOperacao Adicionar(Mercadoria mercadoria, int quantidade = 1)
    {
        if (quantidade <= 0)
            return ResultadoOperacao.Falha("A quantidade deve ser maior que zero.");

        var linha = _linhas.FirstOrDefault(l => l.MercadoriaId == mercadoria.Id);
        var quantidadeFinal = (linha?.Quantidade ?? 0) + quantidade;

        if (mercadoria.Unidade != UnidadeMedida.Quilo && quantidadeFinal > mercadoria.Quantidade)
            return ResultadoOperacao.Falha(
                $"Estoque insuficiente para '{mercadoria.Nome}'. Disponível: {mercadoria.Quantidade}.");

        if (linha is null)
        {
            _linhas.Add(new LinhaCarrinho
            {
                MercadoriaId = mercadoria.Id,
                Nome = mercadoria.Nome,
                Unidade = mercadoria.Unidade,
                PrecoUnitarioCentavos = mercadoria.PrecoVenda,
                Quantidade = quantidadeFinal,
                EstoqueDisponivel = mercadoria.Quantidade
            });
        }
        else
        {
            linha.Quantidade = quantidadeFinal;
            linha.EstoqueDisponivel = mercadoria.Quantidade; // reflete o estoque mais recente
        }

        return ResultadoOperacao.Ok();
    }

    public ResultadoOperacao AlterarQuantidade(int mercadoriaId, int novaQuantidade)
    {
        var linha = _linhas.FirstOrDefault(l => l.MercadoriaId == mercadoriaId);
        if (linha is null)
            return ResultadoOperacao.Falha("Item não está no carrinho.");
        if (novaQuantidade <= 0)
            return ResultadoOperacao.Falha("A quantidade deve ser maior que zero.");
        if (!linha.PorPeso && novaQuantidade > linha.EstoqueDisponivel)
            return ResultadoOperacao.Falha(
                $"Estoque insuficiente para '{linha.Nome}'. Disponível: {linha.EstoqueDisponivel}.");

        linha.Quantidade = novaQuantidade;
        return ResultadoOperacao.Ok();
    }

    public void Remover(int mercadoriaId)
        => _linhas.RemoveAll(l => l.MercadoriaId == mercadoriaId);

    public void Limpar() => _linhas.Clear();

    /// <summary>Converte para os <see cref="ItemCarrinho"/> consumidos por FinalizarVendaAsync (Dia 7).</summary>
    public IReadOnlyList<ItemCarrinho> ParaItensCarrinho()
        => _linhas.Select(l => new ItemCarrinho(l.MercadoriaId, l.Quantidade)).ToList();
}
