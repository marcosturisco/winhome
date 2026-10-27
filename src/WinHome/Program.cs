using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace WinHome;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        // Abrir o WinHome de novo só traz a Home já aberta para a frente.
        using var instancia = new InstanciaUnica();
        var sair = args.Contains("--sair", StringComparer.OrdinalIgnoreCase);
        if (!instancia.Primeira)
        {
            if (sair) instancia.PedirSaida();
            else instancia.ChamarPrimeira();
            return;
        }
        if (sair) return; // não havia Home aberta

        Application.Start(parametros =>
        {
            var contexto = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(contexto);
            _ = new App(instancia, args);
        });    }
}
