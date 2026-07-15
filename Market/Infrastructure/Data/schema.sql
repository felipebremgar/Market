PRAGMA foreign_keys = ON;

-- ---------- Cliente (CPF como PK, Nome obrigatório) ----------
CREATE TABLE Cliente (
    Cpf     TEXT NOT NULL PRIMARY KEY,
    Nome    TEXT NOT NULL,
    Contato TEXT NULL,
    CHECK (length(Cpf) = 11 AND Cpf NOT GLOB '*[^0-9]*')
);
CREATE INDEX IX_Cliente_Nome ON Cliente (Nome);

-- ---------- Mercadoria (preços em centavos) ----------
CREATE TABLE Mercadoria (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome          TEXT    NOT NULL,
    Fornecedor    TEXT    NULL,
    PrecoCusto    INTEGER NOT NULL DEFAULT 0,
    PrecoVenda    INTEGER NOT NULL DEFAULT 0,
    Quantidade    INTEGER NOT NULL DEFAULT 0,
    CodigoBarras  TEXT    NULL,
    Validade      TEXT    NULL,
    DataCadastro  TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
    Ativo         INTEGER NOT NULL DEFAULT 1,
    CHECK (PrecoCusto >= 0),
    CHECK (PrecoVenda >= 0),
    CHECK (Quantidade >= 0),
    CHECK (Ativo IN (0,1))
);
CREATE UNIQUE INDEX UQ_Mercadoria_CodigoBarras
    ON Mercadoria (CodigoBarras) WHERE CodigoBarras IS NOT NULL;
CREATE INDEX IX_Mercadoria_Nome       ON Mercadoria (Nome);
CREATE INDEX IX_Mercadoria_Fornecedor ON Mercadoria (Fornecedor);
CREATE INDEX IX_Mercadoria_Validade   ON Mercadoria (Validade);

-- ---------- Venda (cliente opcional) ----------
CREATE TABLE Venda (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    DataVenda      TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%S','now','localtime')),
    ValorTotal     INTEGER NOT NULL DEFAULT 0,
    ClienteCpf     TEXT    NULL,
    FormaPagamento TEXT    NULL,
    CHECK (ValorTotal >= 0),
    FOREIGN KEY (ClienteCpf) REFERENCES Cliente (Cpf)
        ON UPDATE CASCADE ON DELETE SET NULL
);
CREATE INDEX IX_Venda_DataVenda  ON Venda (DataVenda);
CREATE INDEX IX_Venda_ClienteCpf ON Venda (ClienteCpf);

-- ---------- ItemVenda (preço e custo congelados) ----------
CREATE TABLE ItemVenda (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    VendaId        INTEGER NOT NULL,
    MercadoriaId   INTEGER NOT NULL,
    Quantidade     INTEGER NOT NULL,
    PrecoUnitario  INTEGER NOT NULL,
    PrecoCusto     INTEGER NOT NULL DEFAULT 0,
    CHECK (Quantidade > 0),
    CHECK (PrecoUnitario >= 0),
    FOREIGN KEY (VendaId) REFERENCES Venda (Id) ON DELETE CASCADE,
    FOREIGN KEY (MercadoriaId) REFERENCES Mercadoria (Id)
);
CREATE INDEX IX_ItemVenda_VendaId      ON ItemVenda (VendaId);
CREATE INDEX IX_ItemVenda_MercadoriaId ON ItemVenda (MercadoriaId);

-- ---------- Versão do schema ----------
-- Bancos novos já nascem na versão de schema mais recente; o MigrationRunner só
-- precisa atuar sobre bancos criados antes de uma migração. Mantenha este valor
-- igual a SchemaMigrations.VersaoAlvo.
PRAGMA user_version = 3;
