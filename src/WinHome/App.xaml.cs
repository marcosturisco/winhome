using Microsoft.UI.Xaml;

namespace WinHome;

public partial class App : Application
{
    readonly InstanciaUnica _instancia;
    readonly string[] _args;
    MainWindow? _janela;

    // args: os do instalador ("--restaurar <backup.zip>", "--iniciar-com-windows").
    public App(InstanciaUnica instancia, string[] args)
    {
        _instancia = instancia;
        _args = args;
        InitializeComponent();
        UnhandledException += (_, e) => RegistrarErro(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => RegistrarErro(e.ExceptionObject as Exception);
    }

    // Erros não tratados vão para %APPDATA%\WinHome\erro.log.
    static void RegistrarErro(Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(Configuracao.ConfigService.Pasta);
            File.AppendAllText(Path.Combine(Configuracao.ConfigService.Pasta, "erro.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch (IOException)
        {
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _janela = new MainWindow();
        _instancia.Chamada += () => _janela.DispatcherQueue.TryEnqueue(_janela.Mostrar);
        _instancia.PedidoDeSaida += () => _janela.DispatcherQueue.TryEnqueue(_janela.Sair);
        _janela.Mostrar();
        _janela.AplicarArgumentos(_args);
    }
}
