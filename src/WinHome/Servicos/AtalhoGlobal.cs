using System.Runtime.InteropServices;

namespace WinHome.Servicos;

// Tecla de atalho global (RegisterHotKey) recebida pela própria janela da Home.
public sealed class AtalhoGlobal : IDisposable
{
    const int WM_HOTKEY = 0x0312;
    const int Id = 1;
    const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_WIN = 0x8, MOD_NOREPEAT = 0x4000;

    readonly IntPtr _hwnd;
    bool _registrado;

    public event Action? Pressionado;

    public AtalhoGlobal(MensagensJanela mensagens)
    {
        _hwnd = mensagens.Hwnd;
        mensagens.Adicionar((msg, wParam, _) =>
        {
            if (msg != WM_HOTKEY || wParam != Id) return false;
            Pressionado?.Invoke();
            return true;
        });
    }

    // Troca a tecla. Devolve o erro (texto inválido ou tecla já usada por outro programa), se houver.
    public string? Definir(string texto)
    {
        Liberar();
        if (string.IsNullOrWhiteSpace(texto)) return null;
        if (!Interpretar(texto, out var mods, out var tecla))
            return $"Atalho \"{texto}\" não reconhecido. Exemplo: \"Ctrl+Alt+Space\".";
        _registrado = RegisterHotKey(_hwnd, Id, mods | MOD_NOREPEAT, tecla);
        return _registrado ? null : $"O atalho \"{texto}\" já está em uso por outro programa.";
    }

    static bool Interpretar(string texto, out uint mods, out uint tecla)
    {
        mods = 0;
        tecla = 0;
        foreach (var parte in texto.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (parte.ToLowerInvariant())
            {
                case "ctrl": case "control": mods |= MOD_CONTROL; break;
                case "alt": mods |= MOD_ALT; break;
                case "shift": mods |= MOD_SHIFT; break;
                case "win": mods |= MOD_WIN; break;
                default:
                    if (tecla != 0 || !Tecla(parte, out tecla)) return false;
                    break;
            }
        }
        return tecla != 0;
    }

    static bool Tecla(string nome, out uint vk)
    {
        vk = nome.ToLowerInvariant() switch
        {
            "space" or "espaco" => 0x20,
            "enter" => 0x0D,
            "tab" => 0x09,
            "esc" => 0x1B,
            "home" => 0x24,
            _ => 0,
        };
        if (vk != 0) return true;
        if (nome.Length == 1 && char.IsAsciiLetterOrDigit(nome[0]))
        {
            vk = char.ToUpperInvariant(nome[0]);
            return true;
        }
        if (nome.Length is 2 or 3 && (nome[0] is 'F' or 'f') && int.TryParse(nome[1..], out var n) && n is >= 1 and <= 24)
        {
            vk = (uint)(0x70 + n - 1);
            return true;
        }
        return false;
    }

    void Liberar()
    {
        if (_registrado) UnregisterHotKey(_hwnd, Id);
        _registrado = false;
    }

    public void Dispose() => Liberar();

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
