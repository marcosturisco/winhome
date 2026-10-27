using Microsoft.Win32;

namespace WinHome.Servicos;

// Liga/desliga "iniciar com o Windows" pelo valor WinHome em HKCU\...\Run (sem administrador).
// Desfazer manual: reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v WinHome /f
public static class Inicializacao
{
    const string Chave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Valor = "WinHome";

    // Com a versão instalada (instalador\instalar.ps1) presente, o Windows abre sempre ela — mesmo quando quem está
    // rodando é a de desenvolvimento (bin\Debug), para um teste não trocar o programa do início do Windows.
    static string Programa()
    {
        var instalado = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinHome", "app", "WinHome.exe");
        return File.Exists(instalado) ? instalado : Environment.ProcessPath ?? instalado;
    }

    public static string? Sincronizar(bool ligado)
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(Chave, writable: true);
            if (run is null) return "Chave Run do usuário não encontrada.";
            var comando = $"\"{Programa()}\"";
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
