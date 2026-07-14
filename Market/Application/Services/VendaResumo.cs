using Market.Domain;

namespace Market.Application.Services;

/// <summary>Linha do histórico de vendas (cabeçalho), pronta para exibição.</summary>
public record VendaResumo(
    int Id, DateTime DataVenda, int ValorTotal, string? ClienteCpf, string? ClienteNome)
{
    public string DataTexto => DataVenda.ToString("dd/MM/yyyy HH:mm");
    public string TotalTexto => Moeda.ParaTexto(ValorTotal);
    public string ClienteTexto => ClienteNome ?? "—";
}
