using System.Reflection;

namespace WinHome.Servicos;

// Versão do WinHome (a do <Version> do csproj, que o publicar.ps1 ajusta) para mostrar na tela e na bandeja.
public static class VersaoApp
{
    // "1.0.2" (sem o "+commit" que o .NET acrescenta).
    public static readonly string Numero =
        (typeof(VersaoApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];

    // A versão instalada roda de %LOCALAPPDATA%\WinHome\app; qualquer outro lugar (bin\Debug) é a de desenvolvimento.
    public static readonly bool Desenvolvimento = !string.Equals(
        Path.GetDirectoryName(Environment.ProcessPath),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinHome", "app"),
        StringComparison.OrdinalIgnoreCase);

    public static string Texto => Desenvolvimento ? $"WinHome {Numero} (desenvolvimento)" : $"WinHome {Numero}";

    public static string PaginaDoRelease => $"https://github.com/marcosturisco/winhome/releases/tag/v{Numero}";
}
