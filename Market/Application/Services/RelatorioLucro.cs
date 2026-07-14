using Market.Domain;

namespace Market.Application.Services;

/// <summary>Resumo de lucro no período (centavos em <c>long</c> — somas podem exceder int).</summary>
public record ResumoLucro(long CustoTotalCentavos, long ReceitaTotalCentavos)
{
    public long LucroTotalCentavos => ReceitaTotalCentavos - CustoTotalCentavos;
    public string CustoTexto => Moeda.ParaTexto(CustoTotalCentavos);
    public string ReceitaTexto => Moeda.ParaTexto(ReceitaTotalCentavos);
    public string LucroTexto => Moeda.ParaTexto(LucroTotalCentavos);

    public static ResumoLucro Vazio { get; } = new(0, 0);
}

/// <summary>Lucro por produto no período.</summary>
public record LucroPorProduto(string Nome, int QtdVendida, long CustoCentavos, long ReceitaCentavos)
{
    public long LucroCentavos => ReceitaCentavos - CustoCentavos;
    public string ReceitaTexto => Moeda.ParaTexto(ReceitaCentavos);
    public string CustoTexto => Moeda.ParaTexto(CustoCentavos);
    public string LucroTexto => Moeda.ParaTexto(LucroCentavos);
}

/// <summary>Lucro por dia no período.</summary>
public record LucroPorDia(DateOnly Dia, long ReceitaCentavos, long CustoCentavos)
{
    public long LucroCentavos => ReceitaCentavos - CustoCentavos;
    public string DiaTexto => Dia.ToString("dd/MM/yyyy");
    public string ReceitaTexto => Moeda.ParaTexto(ReceitaCentavos);
    public string CustoTexto => Moeda.ParaTexto(CustoCentavos);
    public string LucroTexto => Moeda.ParaTexto(LucroCentavos);
}
