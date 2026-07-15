using System.IO;
using System.Text;
using ClosedXML.Excel;
using Market.Application.Services;

namespace Market.Tests.Application;

public class RelatorioExportadorTests
{
    private static readonly ResumoLucro Resumo = new(5000, 8000); // custo 50, receita 80
    private static readonly IReadOnlyList<LucroPorProduto> PorProduto = new[]
    {
        new LucroPorProduto("Arroz", 3, 5000, 8000)
    };
    private static readonly IReadOnlyList<LucroPorDia> PorDia = new[]
    {
        new LucroPorDia(new DateOnly(2026, 7, 10), 8000, 5000)
    };

    [Fact]
    public void Pdf_gera_bytes_com_assinatura_valida()
    {
        var bytes = RelatorioExportador.GerarPdf(null, null, Resumo, PorProduto);

        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4)); // assinatura de arquivo PDF
    }

    [Fact]
    public void Excel_gera_planilhas_com_dados()
    {
        var bytes = RelatorioExportador.GerarExcel(null, null, Resumo, PorProduto, PorDia);

        Assert.True(bytes.Length > 0);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        var nomes = workbook.Worksheets.Select(w => w.Name).ToList();
        Assert.Contains("Resumo", nomes);
        Assert.Contains("Por produto", nomes);
        Assert.Contains("Por dia", nomes);
        Assert.Equal("Arroz", workbook.Worksheet("Por produto").Cell(2, 1).GetString());
    }
}
