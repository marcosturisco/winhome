using System.Runtime.InteropServices;

namespace WinHome;

// Mutex por sessão + evento nomeado para a segunda execução pedir à primeira que apareça.
public sealed class InstanciaUnica : IDisposable
{
    readonly Mutex _mutex;
    readonly EventWaitHandle _evento;
    RegisteredWaitHandle? _espera;

    public bool Primeira { get; }

    // Disparado fora da thread da interface.
    public event Action? Chamada;

    public InstanciaUnica()
    {
        _mutex = new Mutex(true, @"Local\WinHome.Instancia", out var nova);
        Primeira = nova;
        _evento = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\WinHome.Mostrar");
        if (Primeira)
            _espera = ThreadPool.RegisterWaitForSingleObject(_evento, (_, _) => Chamada?.Invoke(), null, -1, false);
    }

    public void ChamarPrimeira()
    {
        AllowSetForegroundWindow(-1); // deixa a primeira instância vir para a frente
        _evento.Set();
    }

    public void Dispose()
    {
        _espera?.Unregister(null);
        _evento.Dispose();
        if (Primeira) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(int processId);
}
