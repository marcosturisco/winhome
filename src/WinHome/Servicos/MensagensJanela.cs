using System.Runtime.InteropServices;

namespace WinHome.Servicos;

// Intercepta as mensagens Win32 da janela da Home (atalho global, ícone da bandeja) com um único subclass.
public sealed class MensagensJanela : IDisposable
{
    // Devolve true quando tratou a mensagem (ela não segue para o WinUI).
    public delegate bool Tratador(uint msg, IntPtr wParam, IntPtr lParam);

    const int GWLP_WNDPROC = -4;

    readonly IntPtr _hwnd;
    readonly WndProc _proc; // referência mantida viva enquanto a janela existir
    readonly IntPtr _original;
    readonly List<Tratador> _tratadores = [];

    public IntPtr Hwnd => _hwnd;

    public MensagensJanela(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _proc = Procedimento;
        _original = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_proc));
    }

    public void Adicionar(Tratador tratador) => _tratadores.Add(tratador);

    IntPtr Procedimento(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        foreach (var t in _tratadores)
            if (t(msg, wParam, lParam)) return IntPtr.Zero;
        return CallWindowProc(_original, hwnd, msg, wParam, lParam);
    }

    public void Dispose() => SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _original);

    delegate IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr valor);
    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")] static extern IntPtr CallWindowProc(IntPtr anterior, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
}
