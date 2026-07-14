namespace Market.Infrastructure.Data;

/// <summary>
/// Uma migração de schema: o número sequencial (que vira o valor de
/// <c>PRAGMA user_version</c> ao ser aplicada) e o SQL idempotente que a executa.
/// A migração 1 é a linha de base — o <c>schema.sql</c> já cria tudo desta versão,
/// então seu SQL é vazio (apenas carimba a versão em bancos antigos).
/// </summary>
public sealed record Migration(int Versao, string Descricao, string Sql);

/// <summary>
/// Registro central das migrações de schema, em ordem crescente de versão.
/// A cada nova versão do sistema que altere o banco, adicione aqui uma migração
/// com o próximo número e o <c>ALTER TABLE</c> correspondente.
/// </summary>
public static class SchemaMigrations
{
    public static readonly IReadOnlyList<Migration> Todas = new[]
    {
        // v1.1 — Linha de base do versionamento de schema. Bancos criados pelo
        // schema.sql já nascem nesta versão; bancos anteriores apenas recebem o
        // carimbo de user_version (nenhum DDL é necessário).
        new Migration(1, "Linha de base (versionamento de schema)", string.Empty),
    };

    /// <summary>Maior número de migração conhecido — o alvo para o qual todo banco deve convergir.</summary>
    public static int VersaoAlvo => Todas.Max(m => m.Versao);
}
