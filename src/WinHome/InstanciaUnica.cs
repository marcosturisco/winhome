using System.Runtime.InteropServices;

namespace WinHome;

// Mutex por sessão + eventos nomeados para a segunda execução pedir à primeira que apareça ou que saia
// ("WinHome.exe --sair", usado pelos scripts: sair normalmente devolve os apps ancorados ao lugar).
public sealed class InstanciaUnica : IDisposable
{
    readonly Mutex _mutex;
    readonly EventWaitHandle _evento;
    readonly EventWaitHandle _sair;
    RegisteredWaitHandle? _espera, _esperaSair;

    public bool Primeira { get; }

    // Disparado fora da thread da interface.
    public event Action? Chamada;
    public event Action? PedidoDeSaida;

    public InstanciaUnica()
    {
        _mutex = new Mutex(true, @"Local\WinHome.Instancia", out var nova);
        Primeira = nova;
        _evento = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\WinHome.Mostrar");
        _sair = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\WinHome.Sair");
        if (Primeira)
        {
            _espera = ThreadPool.RegisterWaitForSingleObject(_evento, (_, _) => Chamada?.Invoke(), null, -1, false);
            _esperaSair = ThreadPool.RegisterWaitForSingleObject(_sair, (_, _) => PedidoDeSaida?.Invoke(), null, -1, false);
        }
    }

    public void ChamarPrimeira()
    {
        AllowSetForegroundWindow(-1); // deixa a primeira instância vir para a frente
        _evento.Set();
    }

    public void PedirSaida() => _sair.Set();

    public void Dispose()
    {
        _espera?.Unregister(null);
        _esperaSair?.Unregister(null);
        _evento.Dispose();
        _sair.Dispose();
        if (Primeira) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(int processId);
}
