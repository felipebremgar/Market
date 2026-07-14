using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Market.Infrastructure.Data;

/// <summary>
/// Resolve a connection string do SQLite a partir do appsettings.json, transformando
/// o caminho relativo do arquivo .db em absoluto (contra a pasta do executável).
/// Fonte única usada tanto pelo <see cref="DatabaseInitializer"/> quanto pelo DbContext,
/// garantindo que ambos abram exatamente o mesmo arquivo.
/// </summary>
public static class SqliteConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' não encontrada no appsettings.json.");

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (!Path.IsPathRooted(builder.DataSource))
            builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

        return builder.ToString();
    }

    public static string ResolvePath(IConfiguration configuration) =>
        new SqliteConnectionStringBuilder(Resolve(configuration)).DataSource;
}
