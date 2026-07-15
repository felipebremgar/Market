using System.IO;
using Market.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Infra;

/// <summary>
/// Cobre a inicialização do banco e o indicador de conexão (VerificarConexao),
/// usados pela v1.2 para exibir a pastilha de status na tela inicial.
/// </summary>
public class DatabaseInitializerTests
{
    private static DatabaseInitializer Criar(string dbPath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = new SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString()
            })
            .Build();

        var runner = new MigrationRunner(NullLogger<MigrationRunner>.Instance);
        return new DatabaseInitializer(config, runner, NullLogger<DatabaseInitializer>.Instance);
    }

    [Fact]
    public void VerificarConexao_retorna_true_apos_Initialize()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"market-init-{Guid.NewGuid():N}.db");
        try
        {
            var init = Criar(dbPath);
            init.Initialize();

            Assert.True(init.VerificarConexao());
            Assert.Contains("Cliente", init.GetTableNames());
        }
        finally
        {
            // Pooling=False: o arquivo é liberado ao fechar as conexões, sem precisar do
            // ClearAllPools() global (que atrapalharia testes em paralelo).
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void Initialize_migra_banco_v1_existente_preservando_dados()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"market-mig-{Guid.NewGuid():N}.db");
        var cs = new SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString();
        try
        {
            // Monta um banco no estado v1: schema completo, mas sem a coluna Contato,
            // com um cliente já cadastrado e user_version = 1.
            using (var conn = new SqliteConnection(cs))
            {
                conn.Open();
                var schema = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
                Executar(conn, schema);
                // Remove as colunas adicionadas por migrações posteriores, voltando ao estado v1,
                // e deixa uma venda antiga para conferir o backfill dos totais congelados.
                Executar(conn,
                    "ALTER TABLE Cliente DROP COLUMN Contato;" +
                    "ALTER TABLE Venda DROP COLUMN FormaPagamento;" +
                    "ALTER TABLE Venda DROP COLUMN StatusPagamento;" +
                    "ALTER TABLE Venda DROP COLUMN DataVencimento;" +
                    "ALTER TABLE Venda DROP COLUMN DataBaixa;" +
                    "ALTER TABLE Mercadoria DROP COLUMN Unidade;" +
                    "ALTER TABLE ItemVenda DROP COLUMN Unidade;" +
                    "ALTER TABLE ItemVenda DROP COLUMN SubtotalCentavos;" +
                    "ALTER TABLE ItemVenda DROP COLUMN CustoCentavos;" +
                    "INSERT INTO Cliente (Cpf, Nome) VALUES ('52998224725','Ana');" +
                    "INSERT INTO Mercadoria (Id, Nome, PrecoCusto, PrecoVenda, Quantidade)" +
                    "  VALUES (1,'Arroz',1800,2500,10);" +
                    "INSERT INTO Venda (Id, ValorTotal) VALUES (1, 5000);" +
                    "INSERT INTO ItemVenda (VendaId, MercadoriaId, Quantidade, PrecoUnitario, PrecoCusto)" +
                    "  VALUES (1, 1, 2, 2500, 1800);" +
                    "PRAGMA user_version = 1;");
            }
            // Roda o Initialize real: como o schema já existe, apenas aplica as migrações.
            Criar(dbPath).Initialize();

            using (var conn = new SqliteConnection(cs))
            {
                conn.Open();
                Assert.Equal(SchemaMigrations.VersaoAlvo, MigrationRunner.LerUserVersion(conn));
                Assert.Equal(1, Escalar(conn,
                    "SELECT COUNT(*) FROM pragma_table_info('Cliente') WHERE name='Contato';"));
                Assert.Equal(1, Escalar(conn,
                    "SELECT COUNT(*) FROM pragma_table_info('Venda') WHERE name='FormaPagamento';"));
                Assert.Equal(1, Escalar(conn,
                    "SELECT COUNT(*) FROM pragma_table_info('Venda') WHERE name='DataVencimento';"));
                Assert.Equal("Ana", Escalar(conn, "SELECT Nome FROM Cliente WHERE Cpf='52998224725';"));

                // Backfill da migração 5: o item antigo (2 × R$25,00, custo R$18,00) ganha
                // os totais congelados — o histórico e o relatório continuam exatos.
                Assert.Equal("Unidade", Escalar(conn, "SELECT Unidade FROM ItemVenda WHERE Id=1;"));
                Assert.Equal(5000, Escalar(conn, "SELECT SubtotalCentavos FROM ItemVenda WHERE Id=1;"));
                Assert.Equal(3600, Escalar(conn, "SELECT CustoCentavos FROM ItemVenda WHERE Id=1;"));
            }
        }
        finally
        {
            // Pooling=False: o arquivo é liberado ao fechar as conexões, sem precisar do
            // ClearAllPools() global (que atrapalharia testes em paralelo).
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    private static void Executar(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static object? Escalar(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var v = cmd.ExecuteScalar();
        return v is long l ? (int)l : v;
    }

    [Fact]
    public void Initialize_deixa_o_banco_na_versao_de_schema_alvo()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"market-init-{Guid.NewGuid():N}.db");
        try
        {
            Criar(dbPath).Initialize();

            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString());
            connection.Open();
            Assert.Equal(SchemaMigrations.VersaoAlvo, MigrationRunner.LerUserVersion(connection));
        }
        finally
        {
            // Pooling=False: o arquivo é liberado ao fechar as conexões, sem precisar do
            // ClearAllPools() global (que atrapalharia testes em paralelo).
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
