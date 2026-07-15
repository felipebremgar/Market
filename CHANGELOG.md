# Changelog

Todas as mudanças relevantes do Mercadinho são registradas aqui.
O versionamento segue a cadência acordada: cada melhoria incrementa o *minor*
(`v1.1`, `v1.2` … `v1.10`) e, ao chegar em `v1.10`, o próximo passo é `v2.0`.

## [1.4.0] — Polimento de UX

### Adicionado
- Menu lateral: hover mais visível (fundo + barra de destaque + negrito) e
  indicação da tela ativa (barra ciano + fundo destacado).
- Empty states nos grids vazios (mercadorias, clientes, vendas, itens, relatório),
  via conversor `InverseBooleanToVisibilityConverter` e estilo `EmptyState`.
- Menu de contexto (botão direito) com Editar/Excluir em Manter Mercadorias,
  selecionando a linha sob o cursor.

### Alterado
- Fonte e altura de linha das tabelas maiores (estilo global de `DataGrid`) e
  labels de formulário/filtro aumentadas, para leitura rápida no caixa.
- Versão do app para 1.4.0.

## [1.3.0] — Feedback ao usuário

### Adicionado
- Controle único de notificação `NotificationBanner` com variantes Sucesso/Aviso/Erro,
  ícone e auto-dismiss opcional; tema centralizado em `NotificacaoTema`.
- Helper `BotaoOcupado`: durante ações assíncronas, o botão é desabilitado e exibe
  "Processando…/Salvando…/Filtrando…/Gerando…", restaurando ao final.

### Alterado
- Todas as telas com mensagens (PDV, Cadastro/Manter Mercadorias, Clientes, Histórico,
  Relatório, janelas de cadastro/edição) passam a usar o `NotificationBanner`, unificando
  cores e comportamento; removidos os painéis de mensagem ad-hoc de cada view.
- Botões de finalizar/salvar/filtrar/buscar/gerar exibem estado de processamento.
- Versão do app para 1.3.0.

## [1.2.0] — Tela inicial limpa e status do banco

### Adicionado
- Pastilha de status do banco no rodapé do menu lateral (verde "Banco conectado" /
  vermelho "Banco indisponível"), ao lado da versão.
- Exibição da versão real do app no menu (`AppInfo`, lida do assembly).
- `DatabaseInitializer.VerificarConexao()` — checagem de acessibilidade do banco.

### Alterado
- Tela inicial deixa de exibir o caminho do banco e o dump de tabelas/produtos;
  passa a mostrar apenas boas-vindas e a manutenção (backup).
- Versão do app para 1.2.0.

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
