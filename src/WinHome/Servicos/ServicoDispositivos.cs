using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.UI.Dispatching;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Resultado do último teste de um aparelho: Ms = latência da conexão (null = não respondeu).
public sealed record StatusDispositivo(bool Respondeu, long? Ms, DateTime Quando);

// Testa se a porta de cada aparelho responde: só abre e fecha uma conexão TCP (tempo limite de 2 s), sem enviar
// nada. Roda em paralelo, a cada "intervalo" segundos, enquanto houver um painel Dispositivos na Home.
public static class ServicoDispositivos
{
    static readonly Dictionary<string, StatusDispositivo> Status = [];
    static readonly HashSet<string> Testando = [];
    static DispatcherQueueTimer? _relogio;
    static Func<Dispositivos>? _config;
    static int _paineis; // painéis Dispositivos na tela agora

    public static event Action? Mudou; // na thread da interface

    public static StatusDispositivo? De(Dispositivo d) => Status.GetValueOrDefault(d.Alvo);

    public static bool EmTeste(Dispositivo d) => Testando.Contains(d.Alvo);

    // Chamado pelo painel; o relógio é um só, e segue o intervalo atual da config.
    public static void Acompanhar(Func<Dispositivos> config)
    {
        _paineis++;
        _config = config;
        if (_relogio is null)
        {
            _relogio = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _relogio.Tick += (_, _) => _ = TestarTodos();
        }
        _relogio.Interval = TimeSpan.FromSeconds(Math.Clamp(config().Intervalo, 15, 3600));
        if (!_relogio.IsRunning) _relogio.Start();

        // Ao abrir a Home: testa já o que estiver sem resultado recente.
        var limite = DateTime.Now - _relogio.Interval;
        if (config().Lista.Any(d => De(d) is not { } s || s.Quando < limite)) _ = TestarTodos();
    }

    // Painel fora da tela (Home escondida ou painel removido). Na remontagem da Home o painel novo pode chegar antes
    // de o antigo sair; por isso conta os painéis e só para os testes quando não sobra nenhum.
    public static void Parar()
    {
        if (--_paineis > 0) return;
        _paineis = 0;
        _relogio?.Stop();
    }

    public static async Task TestarTodos()
    {
        if (_config is null) return;
        await Task.WhenAll(_config().Lista.Select(Testar));
    }

    public static async Task Testar(Dispositivo d)
    {
        var alvo = d.Alvo;
        if (string.IsNullOrWhiteSpace(d.Endereco) || !Testando.Add(alvo)) return;
        Mudou?.Invoke();
        long? ms = null;
        try
        {
            using var tcp = new TcpClient();
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var relogio = Stopwatch.StartNew();
            await tcp.ConnectAsync(d.Endereco, d.Porta, limite.Token);
            ms = relogio.ElapsedMilliseconds;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or ArgumentException) { }
        Testando.Remove(alvo);
        Status[alvo] = new StatusDispositivo(ms is not null, ms, DateTime.Now);
        Mudou?.Invoke();
    }
}
