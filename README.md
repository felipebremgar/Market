# Mercadinho — Sistema de Logística

Sistema desktop para um mercadinho: cadastro e controle de mercadorias, vendas (PDV),
histórico e relatório de lucros. **WPF (.NET 10) + EF Core + SQLite.**

## Requisitos

- **Uso final:** Windows 10/11. O executável publicado é *self-contained* — não precisa
  instalar o .NET.
- **Desenvolvimento:** .NET SDK 10.

## Como executar

**Versão publicada (entrega):**
1. Copie a pasta `publish/` para a máquina.
2. Execute `Market.exe`. O banco `mercadinho.db` é criado na primeira execução, ao lado do
   executável, já com dados de exemplo.

**Em desenvolvimento:**
```bash
dotnet run --project Market
```

**Gerar o pacote de entrega** (executável único, sem dependências):
```bash
dotnet publish Market/Market.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Módulos (menu lateral)

- **Início** — status do banco e **backup** (gera cópia carimbada em `backups/`).
- **Cadastro de Mercadoria** — cadastro com leitor de código de barras, validade opcional,
  preços em reais (convertidos para centavos).
- **Manter Mercadorias** — listagem, filtros por todos os campos, edição, exclusão lógica e
  destaques de validade/estoque baixo.
- **Clientes** — cadastro (CPF validado por dígito verificador) e busca.
- **Vendas (PDV)** — carrinho por bipe ou busca, cliente opcional (com cadastro rápido),
  finalização com baixa de estoque transacional e recibo.
- **Histórico de Vendas** — lista filtrável (período, cliente, produto) com detalhe dos itens.
- **Relatório de Lucros** — receita, custo e lucro por período, com detalhamento por produto
  e por dia (calculado sobre os preços congelados na venda).

## Convenções técnicas

- Dinheiro: **INTEGER em centavos** (`R$ 9,90` → `990`); exibição via `Moeda`, cultura pt-BR.
- Datas: **texto ISO-8601**; validade como `YYYY-MM-DD`.
- Exclusão de mercadoria é **lógica** (`Ativo = 0`); itens com vendas nunca são apagados.
- Preço e custo são **congelados** no `ItemVenda` no momento da venda.

## Testes

```bash
dotnet test        # suíte xUnit (banco SQLite real e descartável por teste)
```

Diagnósticos in-app (sem abrir a UI):
- `Market.exe --initdb` — cria/semeia o banco e sai.
- `Market.exe --test-crud` — roda o self-test integrado e grava `crud-selftest.log`.

## Backup e restauração

O botão **Fazer backup do banco** (tela Início) copia `mercadinho.db` para
`backups/mercadinho_backup_AAAAMMDD_HHMMSS.db`. Para restaurar, feche o app e substitua o
`mercadinho.db` pela cópia desejada.
