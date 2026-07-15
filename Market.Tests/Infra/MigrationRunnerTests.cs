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

    // Migrações sintéticas para testar a mecânica do runner sem depender do DDL real.
    private static readonly IReadOnlyList<Migration> Sinteticas = new[]
    {
        new Migration(1, "base", "CREATE TABLE T1 (Id INTEGER);"),
        new Migration(2, "segunda", "CREATE TABLE T2 (Id INTEGER);"),
    };

    [Fact]
    public void Aplicar_eleva_banco_antigo_ate_a_ultima_versao()
    {
        using var connection = AbrirBancoEmMemoria();

        var versaoFinal = CriarRunner().Aplicar(connection, Sinteticas);

        Assert.Equal(2, versaoFinal);
        Assert.Equal(2, MigrationRunner.LerUserVersion(connection));
    }

    [Fact]
    public void Aplicar_e_idempotente_em_banco_ja_atualizado()
    {
        using var connection = AbrirBancoEmMemoria();
        var runner = CriarRunner();

        runner.Aplicar(connection, Sinteticas);
        var segundaExecucao = runner.Aplicar(connection, Sinteticas); // não deve reprocessar nada

        Assert.Equal(2, segundaExecucao);
    }

    [Fact]
    public void Migracoes_reais_elevam_banco_v1_ate_o_alvo()
    {
        using var connection = AbrirBancoEmMemoria();
        // Simula um banco na versão 1 (tabelas-base sem as colunas adicionadas depois).
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                "CREATE TABLE Cliente (Cpf TEXT PRIMARY KEY, Nome TEXT NOT NULL);" +
                "CREATE TABLE Venda (Id INTEGER PRIMARY KEY, ValorTotal INTEGER);" +
                "PRAGMA user_version = 1;";
            cmd.ExecuteNonQuery();
        }

        var versaoFinal = CriarRunner().Aplicar(connection); // migrações reais

        Assert.Equal(SchemaMigrations.VersaoAlvo, versaoFinal);
        Assert.True(ColunaExiste(connection, "Cliente", "Contato"));         // migração 2
        Assert.True(ColunaExiste(connection, "Venda", "FormaPagamento"));    // migração 3
        Assert.True(ColunaExiste(connection, "Venda", "StatusPagamento"));   // migração 4
        Assert.True(ColunaExiste(connection, "Venda", "DataVencimento"));    // migração 4
        Assert.True(ColunaExiste(connection, "Venda", "DataBaixa"));         // migração 4
    }

    private static bool ColunaExiste(SqliteConnection connection, string tabela, string coluna)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tabela}') WHERE name = $c;";
        command.Parameters.AddWithValue("$c", coluna);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
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
