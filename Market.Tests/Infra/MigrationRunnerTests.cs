using Market.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Tests.Infra;

/// <summary>
/// Cobre a infraestrutura de migração de schema (PRAGMA user_version):
/// bancos antigos são elevados à versão alvo, DDL das migrações roda de fato,
/// bancos já atualizados não são tocados e migrações só se aplicam uma vez.
/// </summary>
public class MigrationRunnerTests
{
    private static MigrationRunner CriarRunner() => new(NullLogger<MigrationRunner>.Instance);

    private static SqliteConnection AbrirBancoEmMemoria()
    {
        // Conexão única mantida aberta: o banco :memory: vive enquanto a conexão viver.
        // Pooling=False é essencial: outros testes (BancoDeTeste) chamam
        // SqliteConnection.ClearAllPools() no Dispose, o que descartaria o handle
        // nativo desta conexão se ela participasse do pool compartilhado.
        var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        connection.Open();
        return connection;
    }

    [Fact]
    public void Banco_novo_comeca_em_user_version_zero()
    {
        using var connection = AbrirBancoEmMemoria();
        Assert.Equal(0, MigrationRunner.LerUserVersion(connection));
    }

    [Fact]
    public void Aplicar_eleva_banco_antigo_ate_a_versao_alvo()
    {
        using var connection = AbrirBancoEmMemoria();

        var versaoFinal = CriarRunner().Aplicar(connection);

        Assert.Equal(SchemaMigrations.VersaoAlvo, versaoFinal);
        Assert.Equal(SchemaMigrations.VersaoAlvo, MigrationRunner.LerUserVersion(connection));
    }

    [Fact]
    public void Aplicar_e_idempotente_em_banco_ja_atualizado()
    {
        using var connection = AbrirBancoEmMemoria();
        var runner = CriarRunner();

        runner.Aplicar(connection);
        var segundaExecucao = runner.Aplicar(connection); // não deve reprocessar nada

        Assert.Equal(SchemaMigrations.VersaoAlvo, segundaExecucao);
    }

    [Fact]
    public void Aplicar_executa_o_ddl_da_migracao_pendente()
    {
        using var connection = AbrirBancoEmMemoria();
        var migracoes = new[]
        {
            new Migration(1, "Cria tabela de exemplo", "CREATE TABLE Exemplo (Id INTEGER PRIMARY KEY, Nome TEXT);"),
        };

        var versaoFinal = CriarRunner().Aplicar(connection, migracoes);

        Assert.Equal(1, versaoFinal);
        Assert.True(TabelaExiste(connection, "Exemplo"));
    }

    [Fact]
    public void Aplicar_roda_migracoes_pendentes_em_ordem_crescente()
    {
        using var connection = AbrirBancoEmMemoria();
        var runner = CriarRunner();

        // Já na versão 1; só a migração 2 deve rodar.
        runner.Aplicar(connection, new[] { new Migration(1, "base", string.Empty) });

        var migracoes = new[]
        {
            new Migration(1, "base", "CREATE TABLE NaoDeveRodar (Id INTEGER);"),
            new Migration(2, "adiciona coluna", "CREATE TABLE Nova (Id INTEGER);"),
        };
        var versaoFinal = runner.Aplicar(connection, migracoes);

        Assert.Equal(2, versaoFinal);
        Assert.True(TabelaExiste(connection, "Nova"));
        Assert.False(TabelaExiste(connection, "NaoDeveRodar")); // migração 1 já estava aplicada
    }

    [Fact]
    public void Falha_em_migracao_nao_avanca_user_version()
    {
        using var connection = AbrirBancoEmMemoria();
        var migracoes = new[]
        {
            new Migration(1, "SQL inválido", "ISTO NAO E SQL VALIDO;"),
        };

        Assert.ThrowsAny<SqliteException>(() => CriarRunner().Aplicar(connection, migracoes));
        Assert.Equal(0, MigrationRunner.LerUserVersion(connection)); // rollback preservou a versão
    }

    private static bool TabelaExiste(SqliteConnection connection, string nome)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type='table' AND name=$n);";
        command.Parameters.AddWithValue("$n", nome);
        return Convert.ToBoolean(command.ExecuteScalar());
    }
}
