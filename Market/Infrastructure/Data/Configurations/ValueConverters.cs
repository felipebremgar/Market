using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Market.Infrastructure.Data.Configurations;

/// <summary>
/// Converte tipos de data do CLR para o texto ISO-8601 usado no banco, exatamente no
/// formato produzido pelos defaults do DDL (strftime '%Y-%m-%dT%H:%M:%S' e date()).
/// </summary>
internal static class ValueConverters
{
    // DateTime -> 'YYYY-MM-DDTHH:MM:SS'. Na leitura aceita 'T' ou espaço (Parse flexível).
    public static readonly ValueConverter<DateTime, string> IsoDateTime = new(
        v => v.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        v => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.None));

    // DateOnly -> 'YYYY-MM-DD'. Casa com as comparações contra date('now', ...) do plano.
    public static readonly ValueConverter<DateOnly, string> IsoDate = new(
        v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        v => DateOnly.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture));
}
