using Market.Application;
using System.Text.RegularExpressions;

namespace Market.Tests.Application;

public class AppInfoTests
{
    [Fact]
    public void VersaoCurta_tem_formato_vMajor_Minor_Build()
    {
        Assert.Matches(new Regex(@"^v\d+\.\d+\.\d+$"), AppInfo.VersaoCurta);
    }

    [Fact]
    public void VersaoCurta_reflete_a_versao_do_assembly()
    {
        var v = AppInfo.Versao;
        Assert.Equal($"v{v.Major}.{v.Minor}.{v.Build}", AppInfo.VersaoCurta);
    }

    [Fact]
    public void Versao_do_assembly_e_pelo_menos_1_1()
    {
        // Garante que o versionamento definido no csproj está sendo lido (não é 0.0.0).
        Assert.True(AppInfo.Versao >= new Version(1, 1, 0));
    }
}
