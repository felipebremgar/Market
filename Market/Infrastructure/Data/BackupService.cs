using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Data;

/// <summary>
/// Backup simples do banco: copia o arquivo .db para uma pasta de backups com carimbo
/// de data/hora. Adequado porque as operações usam contextos de vida curta (banco
/// ocioso entre elas) — sem transação aberta no momento da cópia.
/// </summary>
public class BackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IConfiguration configuration, ILogger<BackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Faz o backup do banco atual; retorna o caminho do arquivo gerado.</summary>
    public string RealizarBackup(string? pastaDestino = null)
    {
        var origem = SqliteConnectionString.ResolvePath(_configuration);
        var destino = pastaDestino ?? Path.Combine(AppContext.BaseDirectory, "backups");
        var caminho = Copiar(origem, destino);
        _logger.LogInformation("Backup do banco criado em {Caminho}.", caminho);
        return caminho;
    }

    /// <summary>Copia <paramref name="dbOrigem"/> para <paramref name="pastaDestino"/> com nome carimbado.</summary>
    public static string Copiar(string dbOrigem, string pastaDestino)
    {
        Directory.CreateDirectory(pastaDestino);
        var nome = $"mercadinho_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var caminho = Path.Combine(pastaDestino, nome);
        File.Copy(dbOrigem, caminho, overwrite: false);
        return caminho;
    }
}
