using Market.Domain;

namespace Market.Application.Services;

/// <summary>Uma linha do carrinho (estado de exibição do PDV).</summary>
public class LinhaCarrinho
{
    public int MercadoriaId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public int PrecoUnitarioCentavos { get; init; }
    public int Quantidade { get; internal set; }
    public int EstoqueDisponivel { get; internal set; }

    public int SubtotalCentavos => Quantidade * PrecoUnitarioCentavos;
    public string PrecoUnitarioTexto => Moeda.ParaTexto(PrecoUnitarioCentavos);
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

    /// <summary>Adiciona <paramref name="quantidade"/> unidades; consolida se o produto já estiver no carrinho.</summary>
    public ResultadoOperacao Adicionar(Mercadoria mercadoria, int quantidade = 1)
    {
        if (quantidade <= 0)
            return ResultadoOperacao.Falha("A quantidade deve ser maior que zero.");

        var linha = _linhas.FirstOrDefault(l => l.MercadoriaId == mercadoria.Id);
        var quantidadeFinal = (linha?.Quantidade ?? 0) + quantidade;

        if (quantidadeFinal > mercadoria.Quantidade)
            return ResultadoOperacao.Falha(
                $"Estoque insuficiente para '{mercadoria.Nome}'. Disponível: {mercadoria.Quantidade}.");

        if (linha is null)
        {
            _linhas.Add(new LinhaCarrinho
            {
                MercadoriaId = mercadoria.Id,
                Nome = mercadoria.Nome,
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
        if (novaQuantidade > linha.EstoqueDisponivel)
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
