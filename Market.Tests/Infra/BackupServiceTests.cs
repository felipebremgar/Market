using System.IO;
using Market.Infrastructure.Data;

namespace Market.Tests.Infra;

public class BackupServiceTests
{
    [Fact]
    public void Copiar_gera_arquivo_de_backup_com_o_mesmo_conteudo()
    {
        var origem = Path.Combine(Path.GetTempPath(), $"origem-{Guid.NewGuid():N}.db");
        var destino = Path.Combine(Path.GetTempPath(), $"backups-{Guid.NewGuid():N}");
        File.WriteAllText(origem, "conteudo-do-banco");

        try
        {
            var caminho = BackupService.Copiar(origem, destino);

            Assert.True(File.Exists(caminho));
            Assert.StartsWith("mercadinho_backup_", Path.GetFileName(caminho));
            Assert.Equal("conteudo-do-banco", File.ReadAllText(caminho));
        }
        finally
        {
            File.Delete(origem);
            if (Directory.Exists(destino)) Directory.Delete(destino, recursive: true);
        }
    }
}
