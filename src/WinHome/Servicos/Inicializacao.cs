using Microsoft.Win32;

namespace WinHome.Servicos;

// Liga/desliga "iniciar com o Windows" pelo valor WinHome em HKCU\...\Run (sem administrador).
// Desfazer manual: reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v WinHome /f
public static class Inicializacao
{
    const string Chave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Valor = "WinHome";

    public static string? Sincronizar(bool ligado)
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(Chave, writable: true);
            if (run is null) return "Chave Run do usuário não encontrada.";
            var comando = $"\"{Environment.ProcessPath}\"";
            var atual = run.GetValue(Valor) as string;

            if (ligado && atual != comando) run.SetValue(Valor, comando);
            else if (!ligado && atual is not null) run.DeleteValue(Valor);
            return null;
        }
        catch (Exception ex)
        {
            return $"Não foi possível ajustar a inicialização com o Windows: {ex.Message}";
        }
    }
}
