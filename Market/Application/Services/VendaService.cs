using Market.Domain;
using Market.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Application.Services;

/// <summary>
/// Finalização de venda: operação atômica que baixa estoque, congela preços/custos
/// e calcula o total. Toda a operação corre em UM contexto e UMA transação — qualquer
/// falha faz rollback completo (nada persiste).
/// </summary>
public class VendaService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<VendaService> _logger;

    public VendaService(IDbContextFactory<AppDbContext> contextFactory, ILogger<VendaService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>Monta o recibo de uma venda persistida (itens, total, cliente). Null se não existir.</summary>
    public async Task<ReciboVenda?> ObterReciboAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var venda = await context.Vendas
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Mercadoria)
            .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken);

        if (venda is null)
            return null;

        var itens = venda.Itens
            .Select(i => new ReciboItem(
                i.Mercadoria.Nome, i.Quantidade, i.PrecoUnitario, i.Unidade, i.SubtotalCentavos))
            .ToList();

        return new ReciboVenda(
            venda.Id, venda.DataVenda, venda.Cliente?.Nome, venda.ClienteCpf, venda.ValorTotal, itens,
            venda.Forma, venda.Status, venda.DataVencimento);
    }

    public async Task<ResultadoOperacao> FinalizarVendaAsync(
        string? clienteCpf, IReadOnlyList<ItemCarrinho> itens,
        FormaPagamento forma = FormaPagamento.Dinheiro,
        DateOnly? dataVencimento = null,
        CancellationToken cancellationToken = default)
    {
        if (itens is null || itens.Count == 0)
            return ResultadoOperacao.Falha("O carrinho está vazio.");
        if (itens.Any(i => i.Quantidade <= 0))
            return ResultadoOperacao.Falha("Todas as quantidades devem ser maiores que zero.");

        var cpf = string.IsNullOrWhiteSpace(clienteCpf) ? null : Cpf.Normalizar(clienteCpf);

        if (forma == FormaPagamento.Fiado)
        {
            if (cpf is null)
                return ResultadoOperacao.Falha("Venda fiada exige um cliente.");
            if (dataVencimento is null)
                return ResultadoOperacao.Falha("Venda fiada exige uma data de vencimento.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (cpf is not null &&
                !await context.Clientes.AnyAsync(c => c.Cpf == cpf, cancellationToken))
                return ResultadoOperacao.Falha("Cliente não encontrado.");

            var venda = new Venda
            {
                DataVenda = DateTime.Now,
                ClienteCpf = cpf,
                ValorTotal = 0,
                Forma = forma,
                Status = forma == FormaPagamento.Fiado ? StatusPagamento.Pendente : StatusPagamento.Pago,
                DataVencimento = forma == FormaPagamento.Fiado ? dataVencimento : null
            };
            context.Vendas.Add(venda);
            await context.SaveChangesAsync(cancellationToken); // gera o Id da venda

            long totalCentavos = 0; // long: evita overflow silencioso na soma
            foreach (var item in itens)
            {
                // Mesmo contexto: se o produto se repetir no carrinho, a 2ª linha já enxerga
                // o estoque decrementado pela 1ª (baixa cumulativa).
                var mercadoria = await context.Mercadorias
                    .FirstOrDefaultAsync(m => m.Id == item.MercadoriaId, cancellationToken);

                if (mercadoria is null || !mercadoria.Ativo)
                    return await Cancelar(transaction,
                        $"Mercadoria (id {item.MercadoriaId}) não encontrada.", cancellationToken);

                // Itens por peso (verduras/frutas) não têm acompanhamento de estoque:
                // não validam disponibilidade nem dão baixa.
                if (mercadoria.Unidade != UnidadeMedida.Quilo)
                {
                    if (mercadoria.Quantidade < item.Quantidade)
                        return await Cancelar(transaction,
                            $"Estoque insuficiente para '{mercadoria.Nome}'. Disponível: {mercadoria.Quantidade}.",
                            cancellationToken);

                    mercadoria.Quantidade -= item.Quantidade; // baixa no estoque
                }

                // Totais congelados: calculados uma única vez aqui, para que recibo,
                // histórico e relatório leiam o mesmo valor (sem divergir por arredondamento).
                // Em long: o limite é checado antes de gravar, então o cast para int é seguro.
                var subtotal = CalculoItem.Total(mercadoria.Unidade, item.Quantidade, mercadoria.PrecoVenda);
                var custo = CalculoItem.Total(mercadoria.Unidade, item.Quantidade, mercadoria.PrecoCusto);
                totalCentavos += subtotal;

                if (totalCentavos > int.MaxValue || custo > int.MaxValue)
                    return await Cancelar(transaction,
                        "O valor total da venda excede o limite suportado.", cancellationToken);

                context.ItensVenda.Add(new ItemVenda
                {
                    VendaId = venda.Id,
                    MercadoriaId = mercadoria.Id,
                    Quantidade = item.Quantidade,           // contagem, ou gramas se por peso
                    Unidade = mercadoria.Unidade,           // congela a unidade
                    PrecoUnitario = mercadoria.PrecoVenda,  // congela preço
                    PrecoCusto = mercadoria.PrecoCusto,     // congela custo
                    SubtotalCentavos = (int)subtotal,
                    CustoCentavos = (int)custo
                });
            }

            if (totalCentavos > int.MaxValue)
                return await Cancelar(transaction,
                    "O valor total da venda excede o limite suportado.", cancellationToken);

            venda.ValorTotal = (int)totalCentavos;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Venda {Id} finalizada: {Total} centavos, {Itens} item(ns).",
                venda.Id, totalCentavos, itens.Count);
            return ResultadoOperacao.Ok(venda.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar venda; efetuando rollback.");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<ResultadoOperacao> Cancelar(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        string mensagem, CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return ResultadoOperacao.Falha(mensagem);
    }
}
