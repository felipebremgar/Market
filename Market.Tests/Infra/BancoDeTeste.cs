using System.IO;
using Market.Domain;
using Market.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Market.Tests.Infra;

/// <summary>
/// Banco SQLite temporário por teste: cria um arquivo .db novo, executa o schema.sql
/// real do projeto (FKs, CHECKs e índices ativos) e serve como IDbContextFactory.
/// Descartável — apaga o arquivo ao final.
/// </summary>
public sealed class BancoDeTeste : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly DbContextOptions<AppDbContext> _options;

    public BancoDeTeste()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"market-test-{Guid.NewGuid():N}.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(scriptPath);
        command.ExecuteNonQuery();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());

    // ----- Builders de conveniência -----

    public Mercadoria CriarMercadoria(
        int estoque = 10, int precoVenda = 1000, int precoCusto = 500,
        string? codigoBarras = null, string nome = "Produto", bool ativo = true)
    {
        using var context = CreateDbContext();
        var mercadoria = new Mercadoria
        {
            Nome = nome,
            Quantidade = estoque,
            PrecoVenda = precoVenda,
            PrecoCusto = precoCusto,
            CodigoBarras = codigoBarras,
            Ativo = ativo
        };
        context.Mercadorias.Add(mercadoria);
        context.SaveChanges();
        return mercadoria;
    }

    public Cliente CriarCliente(string cpf, string nome = "Cliente Teste")
    {
        using var context = CreateDbContext();
        var cliente = new Cliente { Cpf = cpf, Nome = nome };
        context.Clientes.Add(cliente);
        context.SaveChanges();
        return cliente;
    }

    /// <summary>Insere uma venda com data explícita e itens (para montar histórico nos testes).</summary>
    public Venda CriarVenda(DateTime data, string? clienteCpf, params (int MercadoriaId, int Qtd, int Preco)[] itens)
    {
        using var context = CreateDbContext();
        var venda = new Venda
        {
            DataVenda = data,
            ClienteCpf = clienteCpf,
            ValorTotal = itens.Sum(i => i.Qtd * i.Preco)
        };
        context.Vendas.Add(venda);
        context.SaveChanges();

        foreach (var item in itens)
            context.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                MercadoriaId = item.MercadoriaId,
                Quantidade = item.Qtd,
                PrecoUnitario = item.Preco,
                PrecoCusto = 0
            });
        context.SaveChanges();
        return venda;
    }

    /// <summary>Como <see cref="CriarVenda"/>, mas com PrecoCusto congelado por item (para relatórios de lucro).</summary>
    public Venda CriarVendaComCusto(
        DateTime data, string? clienteCpf,
        params (int MercadoriaId, int Qtd, int PrecoVenda, int PrecoCusto)[] itens)
    {
        using var context = CreateDbContext();
        var venda = new Venda
        {
            DataVenda = data,
            ClienteCpf = clienteCpf,
            ValorTotal = itens.Sum(i => i.Qtd * i.PrecoVenda)
        };
        context.Vendas.Add(venda);
        context.SaveChanges();

        foreach (var item in itens)
            context.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                MercadoriaId = item.MercadoriaId,
                Quantidade = item.Qtd,
                PrecoUnitario = item.PrecoVenda,
                PrecoCusto = item.PrecoCusto
            });
        context.SaveChanges();
        return venda;
    }

    public int EstoqueAtual(int mercadoriaId)
    {
        using var context = CreateDbContext();
        return context.Mercadorias.AsNoTracking().First(m => m.Id == mercadoriaId).Quantidade;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
