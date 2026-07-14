using System.Reflection;

namespace Market.Application;

/// <summary>
/// Informações do aplicativo derivadas do assembly (fonte única da versão exibida na UI).
/// A versão vem de <c>Version</c>/<c>AssemblyVersion</c> definidos no Market.csproj.
/// </summary>
public static class AppInfo
{
    public static Version Versao =>
        typeof(AppInfo).Assembly.GetName().Version ?? new Version(0, 0, 0);

    /// <summary>Versão curta no formato exibido ao usuário, ex.: "v1.2.0".</summary>
    public static string VersaoCurta => $"v{Versao.Major}.{Versao.Minor}.{Versao.Build}";
}
