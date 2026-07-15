using System.IO;
using ClosedXML.Excel;
using Market.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Market.Application.Services;

/// <summary>
/// Gera o relatório de lucros em PDF (QuestPDF) e Excel (ClosedXML) a partir dos dados
/// já calculados pelo <see cref="RelatorioService"/>.
/// </summary>
public static class RelatorioExportador
{
    static RelatorioExportador()
    {
        // Licença Community do QuestPDF (uso gratuito abaixo do teto de receita).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GerarPdf(
        DateOnly? dataIni, DateOnly? dataFim,
        ResumoLucro resumo, IReadOnlyList<LucroPorProduto> porProduto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Relatório de Lucros — Mercadinho").FontSize(18).Bold();
                    col.Item().Text(PeriodoTexto(dataIni, dataFim)).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Receita: {resumo.ReceitaTexto}");
                        row.RelativeItem().Text($"Custo: {resumo.CustoTexto}");
                        row.RelativeItem().Text($"Lucro: {resumo.LucroTexto}").Bold();
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Produto").Bold();
                            h.Cell().AlignRight().Text("Qtd").Bold();
                            h.Cell().AlignRight().Text("Receita").Bold();
                            h.Cell().AlignRight().Text("Custo").Bold();
                            h.Cell().AlignRight().Text("Lucro").Bold();
                        });

                        foreach (var p in porProduto)
                        {
                            table.Cell().Text(p.Nome);
                            table.Cell().AlignRight().Text(p.QtdVendida.ToString());
                            table.Cell().AlignRight().Text(p.ReceitaTexto);
                            table.Cell().AlignRight().Text(p.CustoTexto);
                            table.Cell().AlignRight().Text(p.LucroTexto);
                        }
                    });
                });

                page.Footer().AlignRight().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    public static byte[] GerarExcel(
        DateOnly? dataIni, DateOnly? dataFim,
        ResumoLucro resumo,
        IReadOnlyList<LucroPorProduto> porProduto,
        IReadOnlyList<LucroPorDia> porDia)
    {
        using var workbook = new XLWorkbook();

        var resumoWs = workbook.Worksheets.Add("Resumo");
        resumoWs.Cell(1, 1).Value = "Relatório de Lucros — Mercadinho";
        resumoWs.Cell(2, 1).Value = PeriodoTexto(dataIni, dataFim);
        resumoWs.Cell(4, 1).Value = "Receita";
        resumoWs.Cell(4, 2).Value = Moeda.ParaReais(resumo.ReceitaTotalCentavos);
        resumoWs.Cell(5, 1).Value = "Custo";
        resumoWs.Cell(5, 2).Value = Moeda.ParaReais(resumo.CustoTotalCentavos);
        resumoWs.Cell(6, 1).Value = "Lucro";
        resumoWs.Cell(6, 2).Value = Moeda.ParaReais(resumo.LucroTotalCentavos);

        var produtoWs = workbook.Worksheets.Add("Por produto");
        EscreverCabecalho(produtoWs, "Produto", "Qtd", "Receita", "Custo", "Lucro");
        var linha = 2;
        foreach (var p in porProduto)
        {
            produtoWs.Cell(linha, 1).Value = p.Nome;
            produtoWs.Cell(linha, 2).Value = p.QtdVendida;
            produtoWs.Cell(linha, 3).Value = Moeda.ParaReais(p.ReceitaCentavos);
            produtoWs.Cell(linha, 4).Value = Moeda.ParaReais(p.CustoCentavos);
            produtoWs.Cell(linha, 5).Value = Moeda.ParaReais(p.LucroCentavos);
            linha++;
        }

        var diaWs = workbook.Worksheets.Add("Por dia");
        EscreverCabecalho(diaWs, "Dia", "Receita", "Custo", "Lucro");
        linha = 2;
        foreach (var d in porDia)
        {
            diaWs.Cell(linha, 1).Value = d.DiaTexto;
            diaWs.Cell(linha, 2).Value = Moeda.ParaReais(d.ReceitaCentavos);
            diaWs.Cell(linha, 3).Value = Moeda.ParaReais(d.CustoCentavos);
            diaWs.Cell(linha, 4).Value = Moeda.ParaReais(d.LucroCentavos);
            linha++;
        }

        foreach (var ws in workbook.Worksheets)
            ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void EscreverCabecalho(IXLWorksheet ws, params string[] titulos)
    {
        for (var i = 0; i < titulos.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = titulos[i];
            cell.Style.Font.Bold = true;
        }
    }

    private static string PeriodoTexto(DateOnly? ini, DateOnly? fim) => (ini, fim) switch
    {
        (null, null) => "Período: todo o histórico",
        (DateOnly i, null) => $"Período: a partir de {i:dd/MM/yyyy}",
        (null, DateOnly f) => $"Período: até {f:dd/MM/yyyy}",
        (DateOnly i, DateOnly f) => $"Período: {i:dd/MM/yyyy} a {f:dd/MM/yyyy}"
    };
}
