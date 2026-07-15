using Market.Domain;

namespace Market.Application.Services;

/// <summary>Linha do histórico de vendas (cabeçalho), pronta para exibição.</summary>
public record VendaResumo(
    int Id, DateTime DataVenda, int ValorTotal, string? ClienteCpf, string? ClienteNome,
    FormaPagamento? Forma, StatusPagamento? Status, DateOnly? DataVencimento)
{
    public string DataTexto => DataVenda.ToString("dd/MM/yyyy HH:mm");
    public string TotalTexto => Moeda.ParaTexto(ValorTotal);
    public string ClienteTexto => ClienteNome ?? "—";
    public string FormaTexto => Forma?.Texto() ?? "—";

    public string SituacaoTexto => Status switch
    {
        StatusPagamento.Pendente => DataVencimento is DateOnly d ? $"Pendente (vence {d:dd/MM/yyyy})" : "Pendente",
        StatusPagamento.Pago => "Pago",
        _ => "—"
    };

    /// <summary>Venda fiada ainda não paga — elegível para dar baixa.</summary>
    public bool PodeReceberBaixa => Forma == FormaPagamento.Fiado && Status == StatusPagamento.Pendente;
}
