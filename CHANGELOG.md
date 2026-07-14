# Changelog

Todas as mudanças relevantes do Mercadinho são registradas aqui.
O versionamento segue a cadência acordada: cada melhoria incrementa o *minor*
(`v1.1`, `v1.2` … `v1.10`) e, ao chegar em `v1.10`, o próximo passo é `v2.0`.

## [1.1.0] — Fundação: versionamento e migrações

### Adicionado
- Versionamento do sistema no `Market.csproj` (`Version`/`AssemblyVersion`/`FileVersion` = 1.1.0).
- Infraestrutura de migração de schema controlada por `PRAGMA user_version`
  (`Migration`, `SchemaMigrations`, `MigrationRunner`), aplicada automaticamente na
  inicialização do banco. Base para as próximas versões que alteram o schema.
- Ícone do executável "M bloco" (`Assets/market.ico`), apontado por `ApplicationIcon`.
- Workflow de GitHub Actions (`.github/workflows/release.yml`): a cada tag `v*`,
  compila, empacota (self-contained win-x64) e publica uma Release com o `.zip`
  para download.
- `CHANGELOG.md` para registrar cada versão.

### Alterado
- `schema.sql` passa a carimbar `PRAGMA user_version = 1` (bancos novos já nascem na
  versão de schema atual).
