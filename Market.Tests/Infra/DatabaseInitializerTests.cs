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
                ["ConnectionStrings:Default"] = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString()
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
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void Initialize_deixa_o_banco_na_versao_de_schema_alvo()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"market-init-{Guid.NewGuid():N}.db");
        try
        {
            Criar(dbPath).Initialize();

            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString());
            connection.Open();
            Assert.Equal(SchemaMigrations.VersaoAlvo, MigrationRunner.LerUserVersion(connection));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
