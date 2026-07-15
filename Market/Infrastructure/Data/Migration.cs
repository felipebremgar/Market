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

        // v1.5 — Contato opcional (telefone ou e-mail) no cliente.
        new Migration(2, "Adiciona coluna Contato em Cliente",
            "ALTER TABLE Cliente ADD COLUMN Contato TEXT NULL;"),

        // v1.7 — Forma de pagamento gravada na venda.
        new Migration(3, "Adiciona coluna FormaPagamento em Venda",
            "ALTER TABLE Venda ADD COLUMN FormaPagamento TEXT NULL;"),

        // v1.8 — Fiado: situação, vencimento e baixa na venda.
        new Migration(4, "Adiciona colunas de fiado em Venda",
            "ALTER TABLE Venda ADD COLUMN StatusPagamento TEXT NULL;" +
            "ALTER TABLE Venda ADD COLUMN DataVencimento TEXT NULL;" +
            "ALTER TABLE Venda ADD COLUMN DataBaixa TEXT NULL;"),
    };

    /// <summary>Maior número de migração conhecido — o alvo para o qual todo banco deve convergir.</summary>
    public static int VersaoAlvo => Todas.Max(m => m.Versao);
}
