using Market.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Data;

/// <summary>
/// Insere dados de teste (1 cliente, 2 mercadorias) para destravar as telas seguintes.
/// Idempotente: só insere quando as tabelas estão vazias.
/// </summary>
public class DataSeeder
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IDbContextFactory<AppDbContext> contextFactory, ILogger<DataSeeder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public void Seed()
    {
        using var context = _contextFactory.CreateDbContext();

        if (context.Clientes.Any() || context.Mercadorias.Any())
        {
            _logger.LogInformation("Seed ignorado: já existem dados no banco.");
            return;
        }

        context.Clientes.Add(new Cliente { Cpf = "12345678901", Nome = "Cliente Teste" });
        context.Mercadorias.AddRange(
            new Mercadoria
            {
                Nome = "Arroz 5kg", Fornecedor = "Fornecedor A",
                PrecoCusto = 1800, PrecoVenda = 2500, Quantidade = 50, CodigoBarras = "7891234567890"
            },
            new Mercadoria
            {
                Nome = "Feijão 1kg", Fornecedor = "Fornecedor B",
                PrecoCusto = 500, PrecoVenda = 790, Quantidade = 80, CodigoBarras = "7899876543210"
            });

        context.SaveChanges();
        _logger.LogInformation("Seed inserido: 1 cliente, 2 mercadorias.");
    }
}
