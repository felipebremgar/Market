using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Data;

/// <summary>
/// Aplica migrações de schema incrementais controladas por <c>PRAGMA user_version</c>.
/// Cada migração pendente (número maior que a versão atual do banco) roda dentro da
/// própria transação e, no mesmo passo, avança o <c>user_version</c> — garantindo que
/// uma falha no meio do caminho não deixe o banco em estado inconsistente.
/// </summary>
public sealed class MigrationRunner
{
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(ILogger<MigrationRunner> logger) => _logger = logger;

    /// <summary>Aplica as migrações registradas em <see cref="SchemaMigrations"/>.</summary>
    public int Aplicar(SqliteConnection connection) => Aplicar(connection, SchemaMigrations.Todas);

    /// <summary>
    /// Aplica as migrações fornecidas ao banco da conexão aberta. Retorna a versão final.
    /// Sobrecarga com lista explícita para permitir testes com migrações sintéticas.
    /// </summary>
    public int Aplicar(SqliteConnection connection, IReadOnlyList<Migration> migracoes)
    {
        var atual = LerUserVersion(connection);
        var pendentes = migracoes.Where(m => m.Versao > atual)
                                  .OrderBy(m => m.Versao)
                                  .ToList();

        if (pendentes.Count == 0)
        {
            _logger.LogInformation("Schema já está na versão {Versao}. Nenhuma migração a aplicar.", atual);
            return atual;
        }

        foreach (var migracao in pendentes)
        {
            using var transaction = connection.BeginTransaction();

            if (!string.IsNullOrWhiteSpace(migracao.Sql))
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = migracao.Sql;
                command.ExecuteNonQuery();
            }

            // user_version não aceita parâmetro; o valor é um inteiro sob nosso controle.
            using (var setVersion = connection.CreateCommand())
            {
                setVersion.Transaction = transaction;
                setVersion.CommandText = $"PRAGMA user_version = {migracao.Versao};";
                setVersion.ExecuteNonQuery();
            }

            transaction.Commit();
            _logger.LogInformation("Migração {Versao} aplicada: {Descricao}", migracao.Versao, migracao.Descricao);
        }

        return LerUserVersion(connection);
    }

    /// <summary>Lê a versão de schema gravada no cabeçalho do banco (0 em bancos ainda não versionados).</summary>
    public static int LerUserVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
