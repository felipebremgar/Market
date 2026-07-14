using Market.Application.Services;
using Market.Domain;
using Market.Tests.Infra;

namespace Market.Tests.Application;

public class HistoricoServiceTests
{
    private const string CpfA = "52998224725";
    private const string CpfB = "11144477735";

    private static HistoricoService CriarServico(BancoDeTeste banco) => new(banco);

    // T38 — sem filtro: todas as vendas, mais recente primeiro
    [Fact]
    public async Task Lista_todas_as_vendas_ordenadas_por_data_desc()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria(nome: "Arroz", precoVenda: 2500);
        banco.CriarVenda(new DateTime(2026, 7, 10, 9, 0, 0), null, (m.Id, 1, 2500));
        banco.CriarVenda(new DateTime(2026, 7, 18, 9, 0, 0), null, (m.Id, 2, 2500));
        banco.CriarVenda(new DateTime(2026, 7, 14, 9, 0, 0), null, (m.Id, 1, 2500));

        var vendas = await CriarServico(banco).BuscarVendasAsync(FiltroVenda.Nenhum);

        Assert.Equal(3, vendas.Count);
        Assert.Equal(new DateTime(2026, 7, 18, 9, 0, 0), vendas[0].DataVenda);
        Assert.Equal(new DateTime(2026, 7, 10, 9, 0, 0), vendas[2].DataVenda);
    }

    // T39 — filtro por período (dia inclusive nas duas pontas)
    [Fact]
    public async Task Filtro_por_periodo_inclui_as_duas_pontas()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria();
        banco.CriarVenda(new DateTime(2026, 7, 10, 12, 0, 0), null, (m.Id, 1, 100));
        banco.CriarVenda(new DateTime(2026, 7, 14, 23, 30, 0), null, (m.Id, 1, 100)); // fim do dia
        banco.CriarVenda(new DateTime(2026, 7, 18, 12, 0, 0), null, (m.Id, 1, 100));

        var vendas = await CriarServico(banco).BuscarVendasAsync(new FiltroVenda
        {
            DataIni = new DateOnly(2026, 7, 12),
            DataFim = new DateOnly(2026, 7, 14)
        });

        Assert.Single(vendas);
        Assert.Equal(14, vendas[0].DataVenda.Day);
    }

    // T40 — filtro por cliente
    [Fact]
    public async Task Filtro_por_cliente()
    {
        using var banco = new BancoDeTeste();
        banco.CriarCliente(CpfA, "Ana");
        banco.CriarCliente(CpfB, "Bruno");
        var m = banco.CriarMercadoria();
        banco.CriarVenda(DateTime.Now, CpfA, (m.Id, 1, 100));
        banco.CriarVenda(DateTime.Now, CpfB, (m.Id, 1, 100));
        banco.CriarVenda(DateTime.Now, null, (m.Id, 1, 100));

        var vendas = await CriarServico(banco).BuscarVendasAsync(new FiltroVenda { ClienteCpf = CpfA });

        Assert.Single(vendas);
        Assert.Equal("Ana", vendas[0].ClienteNome);
    }

    // T41 — filtro por produto (nome parcial)
    [Fact]
    public async Task Filtro_por_produto_retorna_vendas_que_contem_o_item()
    {
        using var banco = new BancoDeTeste();
        var arroz = banco.CriarMercadoria(nome: "Arroz 5kg");
        var feijao = banco.CriarMercadoria(nome: "Feijão 1kg");
        banco.CriarVenda(DateTime.Now, null, (arroz.Id, 1, 2500));
        banco.CriarVenda(DateTime.Now, null, (feijao.Id, 1, 790));
        banco.CriarVenda(DateTime.Now, null, (arroz.Id, 2, 2500), (feijao.Id, 1, 790));

        var vendas = await CriarServico(banco).BuscarVendasAsync(new FiltroVenda { ProdutoNome = "Arroz" });

        Assert.Equal(2, vendas.Count); // as duas que contêm Arroz
    }

    // T42 — detalhe: itens de uma venda com subtotais
    [Fact]
    public async Task Obter_itens_traz_nome_quantidade_e_subtotal()
    {
        using var banco = new BancoDeTeste();
        var arroz = banco.CriarMercadoria(nome: "Arroz");
        var feijao = banco.CriarMercadoria(nome: "Feijão");
        var venda = banco.CriarVenda(DateTime.Now, null, (arroz.Id, 2, 2500), (feijao.Id, 3, 790));

        var itens = await CriarServico(banco).ObterItensAsync(venda.Id);

        Assert.Equal(2, itens.Count);
        var itemArroz = itens.Single(i => i.Nome == "Arroz");
        Assert.Equal(2, itemArroz.Quantidade);
        Assert.Equal(5000, itemArroz.SubtotalCentavos);
    }

    // T43 — venda sem cliente: ClienteNome nulo (LEFT JOIN)
    [Fact]
    public async Task Venda_sem_cliente_tem_cliente_nulo_no_resumo()
    {
        using var banco = new BancoDeTeste();
        var m = banco.CriarMercadoria();
        banco.CriarVenda(DateTime.Now, null, (m.Id, 1, 100));

        var vendas = await CriarServico(banco).BuscarVendasAsync(FiltroVenda.Nenhum);

        Assert.Null(vendas[0].ClienteNome);
        Assert.Equal("—", vendas[0].ClienteTexto);
    }
}
