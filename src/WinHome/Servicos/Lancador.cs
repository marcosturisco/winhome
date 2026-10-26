using System.Diagnostics;
using WinHome.Configuracao;

namespace WinHome.Servicos;

public static class Lancador
{
    // Abre o app pelo AppID do menu Iniciar (Store ou Win32) ou pelo caminho. Devolve o erro, se houver.
    public static string? Abrir(AppItem item)
    {
        try
        {
            ProcessStartInfo info;
            if (!string.IsNullOrWhiteSpace(item.App))
            {
                info = new ProcessStartInfo("explorer.exe", $"shell:AppsFolder\\{item.App}");
            }
            else if (!string.IsNullOrWhiteSpace(item.Caminho))
            {
                var caminho = Environment.ExpandEnvironmentVariables(item.Caminho);
                info = new ProcessStartInfo(caminho)
                {
                    Arguments = item.Args ?? "",
                    WorkingDirectory = item.Pasta is { Length: > 0 } pasta
                        ? Environment.ExpandEnvironmentVariables(pasta)
                        : Path.GetDirectoryName(caminho) ?? "",
                };
            }
            else
            {
                return $"\"{item.Nome}\" não tem \"app\" nem \"caminho\" na config.";
            }

            info.UseShellExecute = true;
            Process.Start(info);
            return null;
        }
        catch (Exception ex)
        {
            return $"Não foi possível abrir \"{item.Nome}\": {ex.Message}";
        }
    }
}
