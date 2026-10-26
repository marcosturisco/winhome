using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace WinHome;

public static class Program
{
    [STAThread]
    static void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        // Abrir o WinHome de novo só traz a Home já aberta para a frente.
        using var instancia = new InstanciaUnica();
        if (!instancia.Primeira)
        {
            instancia.ChamarPrimeira();
            return;
        }

        Application.Start(parametros =>
        {
            var contexto = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(contexto);
            _ = new App(instancia);
        });    }
}
