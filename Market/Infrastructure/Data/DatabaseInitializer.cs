using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Data;

/// <summary>
/// Cria o arquivo do banco SQLite, executa o script DDL na primeira execução
/// e valida a conexão com um teste simples de escrita/leitura.
/// </summary>
public class DatabaseInitializer
{
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _logger = logger;
        _connectionString = SqliteConnectionString.Resolve(configuration);
        DatabasePath = SqliteConnectionString.ResolvePath(configuration);
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open(); // cria o arquivo .db se ainda não existir

        if (!SchemaExists(connection))
        {
            _logger.LogInformation("Esquema não encontrado. Executando script DDL...");
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Data", "schema.sql");

            using var command = connection.CreateCommand();
            command.CommandText = File.ReadAllText(scriptPath);
            command.ExecuteNonQuery();

            _logger.LogInformation("Esquema criado com sucesso.");
        }

        SmokeTest(connection);
        _logger.LogInformation("Banco de dados pronto em {Path}", DatabasePath);
    }

    public IReadOnlyList<string> GetTableNames()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";

        var tables = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static bool SchemaExists(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Cliente');";
        return Convert.ToBoolean(command.ExecuteScalar());
    }

    /// <summary>
    /// Insere um registro dentro de uma transação, lê de volta e desfaz —
    /// valida escrita e leitura sem deixar resíduo no banco.
    /// </summary>
    private void SmokeTest(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO Cliente (Cpf, Nome) VALUES ('00000000000', 'Teste Conexão');";
            insert.ExecuteNonQuery();
        }

        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = "SELECT Nome FROM Cliente WHERE Cpf = '00000000000';";
            if ((string?)select.ExecuteScalar() != "Teste Conexão")
                throw new InvalidOperationException("Falha no teste de escrita/leitura do banco.");
        }

        transaction.Rollback();
        _logger.LogInformation("Teste de escrita/leitura concluído com sucesso.");
    }
}
