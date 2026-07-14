using Market.Application.Services;
using Market.Domain;
using Market.Infrastructure.Data.Repositories;
using Market.Tests.Infra;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Application;

/// <summary>
/// Fluxo completo do sistema + checklist de consistência do Dia 10:
/// cadastra → vende → baixa estoque → histórico → relatório, com os números batendo.
/// </summary>
public class FluxoCompletoTests
{
    private const string CpfMaria = "52998224725";

    [Fact]
    public async Task Cadastra_vende_baixa_estoque_historico_e_relatorio_sao_consistentes()
    {
        using var banco = new BancoDeTeste();
        var mercRepo = new MercadoriaRepository(banco);
        var cliRepo = new ClienteRepository(banco);
        var mercServ = new MercadoriaService(mercRepo, NullLogger<MercadoriaService>.Instance);
        var cliServ = new ClienteService(cliRepo, NullLogger<ClienteService>.Instance);
        var vendaServ = new VendaService(banco, NullLogger<VendaService>.Instance);
        var histServ = new HistoricoService(banco);
        var relServ = new RelatorioService(banco);

        // 1) Cadastra dois produtos e um cliente
        var arroz = await mercServ.CadastrarAsync(new CadastroMercadoriaDados
        { Nome = "Arroz", PrecoCustoReais = 18m, PrecoVendaReais = 25m, Quantidade = 50, CodigoBarras = "111" });
        var feijao = await mercServ.CadastrarAsync(new CadastroMercadoriaDados
        { Nome = "Feijão", PrecoCustoReais = 5m, PrecoVendaReais = 7.90m, Quantidade = 80, CodigoBarras = "222" });
        await cliServ.CadastrarAsync(CpfMaria, "Maria");
        var arrozId = arroz.IdGerado!.Value;
        var feijaoId = feijao.IdGerado!.Value;

        // Checklist: código de barras duplicado é bloqueado no cadastro
        var duplicado = await mercServ.CadastrarAsync(new CadastroMercadoriaDados
        { Nome = "Cópia", PrecoVendaReais = 1m, Quantidade = 1, CodigoBarras = "111" });
        Assert.False(duplicado.Sucesso);

        // 2) Vende 2 Arroz + 3 Feijão para Maria
        var venda = await vendaServ.FinalizarVendaAsync(CpfMaria, new[]
        {
            new ItemCarrinho(arrozId, 2),
            new ItemCarrinho(feijaoId, 3)
        });
        Assert.True(venda.Sucesso);

        // Checklist: estoque após = inicial − quantidades vendidas
        Assert.Equal(48, banco.EstoqueAtual(arrozId));
        Assert.Equal(77, banco.EstoqueAtual(feijaoId));

        // 3) Histórico lista a venda com o cliente
        var historico = await histServ.BuscarVendasAsync(FiltroVenda.Nenhum);
        var vendaHist = Assert.Single(historico);
        Assert.Equal("Maria", vendaHist.ClienteNome);
        var valorTotal = vendaHist.ValorTotal; // 2*2500 + 3*790 = 7370
        Assert.Equal(7370, valorTotal);

        // 4) Checklist: soma dos ValorTotal = soma das receitas no relatório
        var resumo = await relServ.ResumoAsync(null, null);
        Assert.Equal(valorTotal, resumo.ReceitaTotalCentavos);
        Assert.Equal(2 * 1800 + 3 * 500, resumo.CustoTotalCentavos); // 5100
        Assert.Equal(valorTotal - 5100, resumo.LucroTotalCentavos);   // 2270

        // 5) Checklist: venda de cliente excluído aparece SEM cliente (não some)
        var maria = await cliRepo.GetByIdAsync(CpfMaria);
        await cliRepo.DeleteAsync(maria!);

        var historicoApos = await histServ.BuscarVendasAsync(FiltroVenda.Nenhum);
        var vendaApos = Assert.Single(historicoApos);   // não sumiu
        Assert.Null(vendaApos.ClienteNome);              // sem cliente (SET NULL)
        Assert.Null(vendaApos.ClienteCpf);
        Assert.Equal(valorTotal, vendaApos.ValorTotal);  // valor preservado
    }
}
