using Market.Domain;
using Market.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Application.Services;

/// <summary>Uma venda fiada ainda pendente, pronta para exibição na agenda da tela inicial.</summary>
public record FiadoPendente(
    int VendaId, string? ClienteNome, string? ClienteCpf, int ValorCentavos, DateOnly DataVencimento)
{
    public string ClienteTexto => ClienteNome ?? "—";
    public string ValorTexto => Moeda.ParaTexto(ValorCentavos);
    public string VencimentoTexto => DataVencimento.ToString("dd/MM/yyyy");

    public int DiasParaVencer => DataVencimento.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
    public bool Vencido => DiasParaVencer < 0;

    public string SituacaoTexto => Vencido
        ? $"Vencido há {-DiasParaVencer} dia(s)"
        : DiasParaVencer == 0 ? "Vence hoje" : $"Vence em {DiasParaVencer} dia(s)";
}

/// <summary>
/// Gestão do fiado: lista as vendas fiadas pendentes (agenda/alertas) e registra a baixa
/// (pagamento da dívida), que é o que faz a venda entrar no relatório de lucros.
/// </summary>
public class FiadoService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<FiadoService> _logger;

    public FiadoService(IDbContextFactory<AppDbContext> contextFactory, ILogger<FiadoService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>Vendas fiadas ainda pendentes, ordenadas pelo vencimento mais próximo.</summary>
    public async Task<IReadOnlyList<FiadoPendente>> ListarPendentesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var pendentes = await context.Vendas.AsNoTracking()
            .Include(v => v.Cliente)
            .Where(v => v.Forma == FormaPagamento.Fiado
                        && v.Status == StatusPagamento.Pendente
                        && v.DataVencimento != null)
            .OrderBy(v => v.DataVencimento)
            .ToListAsync(cancellationToken);

        return pendentes
            .Select(v => new FiadoPendente(
                v.Id, v.Cliente?.Nome, v.ClienteCpf, v.ValorTotal, v.DataVencimento!.Value))
            .ToList();
    }

    /// <summary>
    /// Registra o pagamento (baixa) de uma venda fiada: marca como Paga e grava a data de hoje.
    /// A partir daqui a venda passa a contar no relatório de lucros.
    /// </summary>
    public async Task<ResultadoOperacao> DarBaixaAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var venda = await context.Vendas.FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken);

        if (venda is null)
            return ResultadoOperacao.Falha("Venda não encontrada.");
        if (venda.Forma != FormaPagamento.Fiado)
            return ResultadoOperacao.Falha("A venda não é fiada.");
        if (venda.Status == StatusPagamento.Pago)
            return ResultadoOperacao.Falha("Esta venda já recebeu baixa.");

        venda.Status = StatusPagamento.Pago;
        venda.DataBaixa = DateTime.Now;
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Baixa de fiado registrada na venda {Id}.", vendaId);
        return ResultadoOperacao.Ok(vendaId);
    }
}
