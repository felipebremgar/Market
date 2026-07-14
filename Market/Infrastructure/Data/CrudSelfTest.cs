using System.Text;
using Market.Application.Services;
using Market.Domain;
using Market.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Data;

/// <summary>
/// Exercita Create/Read/Update/Delete pelos repositórios contra o banco real, provando
/// o entregável do Dia 2. Opera sobre um registro descartável e o remove ao final,
/// deixando o banco no estado em que estava. Acionado por <c>Market.exe --test-crud</c>.
/// </summary>
public class CrudSelfTest
{
    private const string CpfTeste = "99999999999";

    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<Mercadoria> _mercadorias;
    private readonly IRepository<Venda> _vendas;
    private readonly MercadoriaService _mercadoriaService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<CrudSelfTest> _logger;

    public CrudSelfTest(
        IRepository<Cliente> clientes,
        IRepository<Mercadoria> mercadorias,
        IRepository<Venda> vendas,
        MercadoriaService mercadoriaService,
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<CrudSelfTest> logger)
    {
        _clientes = clientes;
        _mercadorias = mercadorias;
        _vendas = vendas;
        _mercadoriaService = mercadoriaService;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<string> RunAsync()
    {
        var report = new StringBuilder();
        var ok = true;

        void Check(string nome, bool condicao)
        {
            ok &= condicao;
            report.AppendLine($"  [{(condicao ? "OK" : "FALHA")}] {nome}");
        }

        // Garante estado limpo caso uma execução anterior tenha sido interrompida.
        var residual = await _clientes.GetByIdAsync(CpfTeste);
        if (residual is not null) await _clientes.DeleteAsync(residual);

        // CREATE
        await _clientes.AddAsync(new Cliente { Cpf = CpfTeste, Nome = "CRUD Teste" });
        var criado = await _clientes.GetByIdAsync(CpfTeste);
        Check("CREATE + READ por chave", criado is { Nome: "CRUD Teste" });

        // UPDATE
        criado!.Nome = "CRUD Atualizado";
        await _clientes.UpdateAsync(criado);
        var atualizado = await _clientes.GetByIdAsync(CpfTeste);
        Check("UPDATE refletido na releitura", atualizado!.Nome == "CRUD Atualizado");

        // DELETE
        await _clientes.DeleteAsync(atualizado);
        var apagado = await _clientes.GetByIdAsync(CpfTeste);
        Check("DELETE remove o registro", apagado is null);

        // READ de coleção (mercadorias do seed) + conversão de moeda
        var mercadorias = await _mercadorias.GetAllAsync();
        Check("READ coleção retorna o seed (>= 2 mercadorias)", mercadorias.Count >= 2);

        var arroz = mercadorias.FirstOrDefault(m => m.Nome == "Arroz 5kg");
        Check("Preço em centavos preservado (Arroz = 2500)", arroz?.PrecoVenda == 2500);
        Check("Custo em centavos preservado (Arroz = 1800)", arroz?.PrecoCusto == 1800);
        Check("DataCadastro preenchida pelo banco", arroz is not null && arroz.DataCadastro.Year >= 2026);
        if (arroz is not null)
            report.AppendLine($"       Arroz 5kg -> {Moeda.ParaTexto(arroz.PrecoVenda)} (estoque {arroz.Quantidade})");

        // Mapeamento de DateOnly (Validade) — round-trip por um registro descartável.
        var validade = new DateOnly(2026, 12, 31);
        var temp = await _mercadorias.AddAsync(new Mercadoria
        {
            Nome = "Item Temporário CRUD", PrecoVenda = 199, Quantidade = 1, Validade = validade
        });
        var tempLido = await _mercadorias.GetByIdAsync(temp.Id);
        Check("Validade (DateOnly) round-trip", tempLido?.Validade == validade);
        await _mercadorias.DeleteAsync(tempLido!);
        var tempApagado = await _mercadorias.GetByIdAsync(temp.Id);
        Check("Mercadoria temporária removida", tempApagado is null);

        // ---- Venda + ItemVenda: relacionamentos e regras de exclusão ----
        var feijao = mercadorias.First(m => m.Nome == "Feijão 1kg");

        await _clientes.AddAsync(new Cliente { Cpf = CpfTeste, Nome = "Cliente Venda" });

        // Grafo inteiro (venda + itens) persistido em um único Add.
        var venda = await _vendas.AddAsync(new Venda
        {
            ClienteCpf = CpfTeste,
            ValorTotal = 2 * arroz!.PrecoVenda + feijao.PrecoVenda,
            Itens =
            {
                new ItemVenda { MercadoriaId = arroz.Id, Quantidade = 2,
                                PrecoUnitario = arroz.PrecoVenda, PrecoCusto = arroz.PrecoCusto },
                new ItemVenda { MercadoriaId = feijao.Id, Quantidade = 1,
                                PrecoUnitario = feijao.PrecoVenda, PrecoCusto = feijao.PrecoCusto }
            }
        });
        Check("Venda + itens persistidos em grafo (Id gerado)", venda.Id > 0 && venda.Itens.All(i => i.Id > 0));
        Check("DataVenda preenchida pelo default do banco", venda.DataVenda > DateTime.MinValue);

        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var vendaLida = await context.Vendas
                .Include(v => v.Itens).ThenInclude(i => i.Mercadoria)
                .Include(v => v.Cliente)
                .AsNoTracking()
                .FirstAsync(v => v.Id == venda.Id);
            Check("READ venda com Include de itens e cliente",
                vendaLida.Itens.Count == 2 && vendaLida.Cliente!.Nome == "Cliente Venda");
            Check("Valores congelados corretos no item",
                vendaLida.Itens.First(i => i.MercadoriaId == arroz.Id) is { PrecoUnitario: 2500, PrecoCusto: 1800 });

            // Mercadoria com vendas não pode ser apagada fisicamente (FK Restrict).
            context.Mercadorias.Attach(new Mercadoria { Id = arroz.Id }).State = EntityState.Deleted;
            var restrictOk = false;
            try { await context.SaveChangesAsync(); }
            catch (DbUpdateException) { restrictOk = true; }
            Check("DELETE de mercadoria com vendas é bloqueado (Restrict)", restrictOk);
        }

        // ON DELETE SET NULL: excluir o cliente mantém a venda, sem cliente.
        var clienteVenda = await _clientes.GetByIdAsync(CpfTeste);
        await _clientes.DeleteAsync(clienteVenda!);
        var vendaSemCliente = await _vendas.GetByIdAsync(venda.Id);
        Check("Excluir cliente mantém a venda com ClienteCpf nulo (SET NULL)",
            vendaSemCliente is { ClienteCpf: null });

        // ON DELETE CASCADE: excluir a venda apaga seus itens.
        await _vendas.DeleteAsync(vendaSemCliente!);
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var itensRestantes = await context.ItensVenda.CountAsync(i => i.VendaId == venda.Id);
            Check("Excluir venda apaga os itens (CASCADE)", itensRestantes == 0);
        }

        // ---- MercadoriaService (Dia 3): validação, centavos e código único ----
        var cadastroOk = await _mercadoriaService.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Macarrão 500g", Fornecedor = "Fornecedor C",
            PrecoCustoReais = 3.50m, PrecoVendaReais = 5.90m, Quantidade = 40,
            CodigoBarras = "7890000000123", Validade = new DateOnly(2027, 1, 31)
        });
        Check("Service: cadastro válido retorna sucesso", cadastroOk.Sucesso);

        Mercadoria? cadastrada = cadastroOk.IdGerado is int id ? await _mercadorias.GetByIdAsync(id) : null;
        Check("Service: preço em reais convertido para centavos (5,90 -> 590)",
            cadastrada?.PrecoVenda == 590 && cadastrada?.PrecoCusto == 350);
        Check("Service: validade persistida (DateOnly)",
            cadastrada?.Validade == new DateOnly(2027, 1, 31));

        var nomeVazio = await _mercadoriaService.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "   ", PrecoVendaReais = 1m, Quantidade = 1
        });
        Check("Service: nome vazio rejeitado", !nomeVazio.Sucesso && nomeVazio.Erros.Any(e => e.Contains("nome", StringComparison.OrdinalIgnoreCase)));

        var precoNegativo = await _mercadoriaService.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Item Inválido", PrecoVendaReais = -1m, Quantidade = 1
        });
        Check("Service: preço negativo rejeitado", !precoNegativo.Sucesso);

        var qtdNegativa = await _mercadoriaService.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Item Inválido", PrecoVendaReais = 1m, Quantidade = -5
        });
        Check("Service: quantidade negativa rejeitada", !qtdNegativa.Sucesso);

        var codigoDuplicado = await _mercadoriaService.CadastrarAsync(new CadastroMercadoriaDados
        {
            Nome = "Cópia do Arroz", PrecoVendaReais = 10m, Quantidade = 1,
            CodigoBarras = "7891234567890" // já existe no seed
        });
        Check("Service: código de barras duplicado rejeitado",
            !codigoDuplicado.Sucesso && codigoDuplicado.Erros.Any(e => e.Contains("código de barras")));

        // Limpa a mercadoria cadastrada pelo teste.
        if (cadastrada is not null) await _mercadorias.DeleteAsync(cadastrada);

        var resultado = ok ? "PASSOU" : "FALHOU";
        report.Insert(0, $"CRUD self-test: {resultado}{Environment.NewLine}");
        _logger.Log(ok ? LogLevel.Information : LogLevel.Error, "CRUD self-test: {Resultado}", resultado);
        return report.ToString();
    }
}
