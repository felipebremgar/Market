# Changelog

Todas as mudanças relevantes do Mercadinho são registradas aqui.
O versionamento segue a cadência acordada: cada melhoria incrementa o *minor*
(`v1.1`, `v1.2` … `v1.10`) e, ao chegar em `v1.10`, o próximo passo é `v2.0`.

## [2.2.0] — Máscara de CPF e correção da lista de clientes

### Adicionado
- Máscara de CPF (000.000.000-00) nos campos exclusivos de CPF: cadastro/edição de
  cliente, busca de clientes e filtro do histórico. Aceita só dígitos, formata enquanto
  digita e preserva o cursor. O campo "Cliente" do PDV ficou de fora de propósito —
  ele aceita CPF **ou** nome, e a máscara quebraria a busca por nome.

### Corrigido
- A listagem de clientes não refletia o cadastro novo quando havia um filtro de busca
  ativo: o refresh reaplicava o filtro e escondia o cliente recém-criado. Agora o
  cadastro limpa os filtros antes de recarregar.
- A atualização era "fire-and-forget" (sem `await`), tornando o momento do refresh
  não-determinístico e engolindo falhas; agora é aguardada.

### Adicionado
- Após cadastrar/editar, o cliente é selecionado e trazido para a visão (`ScrollIntoView`),
  com notificação de sucesso — confirmação visual de que a lista atualizou.

## [2.1.0] — Instalação limpa (sem dados de teste)

### Removido
- `DataSeeder`: o app não insere mais o cliente e as mercadorias de teste
  ("Cliente Teste", "Arroz 5kg", "Feijão 1kg") em bancos novos. Era andaime do início do
  projeto; instalações novas agora nascem sem nenhum dado.

### Alterado
- Versão do app para 2.1.0.

## [2.0.0] — Relatórios avançados

### Adicionado
- Exportação do relatório de lucros em **PDF** (QuestPDF) e **Excel** (ClosedXML),
  via `RelatorioExportador` e diálogo de salvar arquivo.
- **Gráfico de barras** de lucro por dia no relatório (controle nativo `GraficoBarras`,
  sem dependências — barras verdes/vermelhas com linha de base no zero).
- Total somado do período no histórico de vendas.

### Alterado
- Dependências novas: QuestPDF e ClosedXML.
- Versão do app para 2.0.0.

## [1.10.0] — Recibos

### Adicionado
- Reabertura do recibo de uma venda a partir do histórico (botão "Ver recibo",
  duplo-clique e menu de contexto), em modo "Recibo — Venda #N".
- Recibo simples padronizado (produtos, quantidade, preço, subtotal, total) com a forma
  de pagamento — no PDV e no histórico.

### Alterado
- `ReciboVenda`/`ObterReciboAsync` passam a incluir forma, situação e vencimento;
  `ReciboWindow` monta a linha de pagamento a partir da venda persistida.
- Versão do app para 1.10.0.

## [1.9.0] — Venda fiada (baixa e alertas)

### Adicionado
- Dar baixa nas vendas fiadas pelo histórico (botão e menu de contexto): marca a venda
  como paga e grava a data (`FiadoService.DarBaixaAsync`).
- Tela inicial: agenda das vendas fiadas pendentes (ordenada por vencimento, vencidas em
  destaque) e notificação de vencidas / a vencer nos próximos 7 dias.

### Alterado
- Relatório de lucros passa a **excluir fiados pendentes**: só entram após a baixa.
- Versão do app para 1.9.0.

## [1.8.0] — Venda fiada (registro)

### Adicionado
- Forma de pagamento **Fiado**, com data de vencimento, no recebimento; a opção só é
  habilitada quando há cliente selecionado (fiado exige cliente).
- Situação da venda (Pendente/Pago) e coluna "Situação" no histórico, com o vencimento
  das fiadas pendentes.
- Migração de schema #4 (`StatusPagamento`, `DataVencimento`, `DataBaixa` na `Venda`).

### Alterado
- `VendaService.FinalizarVendaAsync` recebe o vencimento e grava fiado como Pendente
  (com prazo) e as demais formas como Pago; recibo mostra o vencimento do fiado.
- `schema.sql` cria as colunas de fiado e carimba `user_version = 4`.
- Versão do app para 1.8.0.

## [1.7.0] — Forma de pagamento na venda

### Adicionado
- Forma de pagamento (Dinheiro/Cartão/Pix) persistida na venda e exibida como coluna
  "Pagamento" no histórico.
- Migração de schema #3 (`ALTER TABLE Venda ADD COLUMN FormaPagamento`).

### Alterado
- Enum `FormaPagamento` movido para o Domain (com extensão de texto reutilizável);
  `VendaService.FinalizarVendaAsync` passa a receber e gravar a forma.
- `schema.sql` cria `Venda.FormaPagamento` e carimba `user_version = 3`.
- Versão do app para 1.7.0.

## [1.6.0] — Cadastro de mercadorias inteligente

### Adicionado
- Sugestão de preço de venda por margem de lucro (%) editável (`Precificacao`).
- Margem atual calculada e exibida ao vivo; aviso de margem negativa (venda abaixo
  do custo) com confirmação ao salvar.
- Validação: a validade de mercadoria nova não aceita data no passado (DatePicker +
  checagem no salvar).

### Corrigido
- Bug ao digitar a quantidade: os campos numéricos passam a selecionar todo o conteúdo
  ao receber foco, evitando o "0" inicial preso na frente do número (`SelecionarTudoAoFocar`).

### Alterado
- Versão do app para 1.6.0.

## [1.5.0] — Clientes: contato e edição

### Adicionado
- Contato opcional (telefone ou e-mail) no cliente, com validação (`Domain.Contato`).
- Edição de cliente: a janela de cadastro passa a abrir em modo de edição (CPF travado),
  acionável por botão, duplo-clique ou menu de contexto na lista.
- Coluna Contato na listagem de clientes.
- Migração de schema #2 (`ALTER TABLE Cliente ADD COLUMN Contato`) — primeira migração
  incremental aplicada a bancos existentes via `PRAGMA user_version`.

### Alterado
- `ClienteService.CadastrarAsync` aceita contato; novo `AtualizarAsync`.
- `schema.sql` cria `Cliente.Contato` e carimba `user_version = 2`.
- Versão do app para 1.5.0.

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
