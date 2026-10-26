using System.Text.Json;
using Microsoft.UI.Dispatching;

namespace WinHome.Configuracao;

// Carrega %APPDATA%\WinHome\config.json (criando a partir do padrão) e avisa quando o arquivo muda.
public sealed class ConfigService : IDisposable
{
    public static readonly string Pasta =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinHome");
    public static readonly string Arquivo = Path.Combine(Pasta, "config.json");
    static readonly string Padrao = Path.Combine(AppContext.BaseDirectory, "Assets", "config.padrao.json");

    static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    readonly DispatcherQueue _fila;
    readonly DispatcherQueueTimer _espera;
    FileSystemWatcher? _observador;

    public Config Atual { get; private set; } = new();

    // Chamado na thread da interface: (config nova, null) ou (config anterior, mensagem de erro).
    public event Action<Config, string?>? Mudou;

    public ConfigService(DispatcherQueue fila)
    {
        _fila = fila;
        // Editores salvam em várias etapas; espera o arquivo assentar antes de reler.
        _espera = fila.CreateTimer();
        _espera.Interval = TimeSpan.FromMilliseconds(300);
        _espera.IsRepeating = false;
        _espera.Tick += (_, _) => Recarregar();
    }

    public string? Carregar()
    {
        Directory.CreateDirectory(Pasta);
        if (!File.Exists(Arquivo) && File.Exists(Padrao))
            File.Copy(Padrao, Arquivo);

        var erro = Ler();
        _observador = new FileSystemWatcher(Pasta, "config.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _observador.Changed += (_, _) => Agendar();
        _observador.Created += (_, _) => Agendar();
        _observador.Renamed += (_, _) => Agendar();
        return erro;
    }

    void Agendar() => _fila.TryEnqueue(() => { _espera.Stop(); _espera.Start(); });

    void Recarregar() => Mudou?.Invoke(Atual, Ler());

    string? Ler()
    {
        try
        {
            if (!File.Exists(Arquivo)) return null;
            var texto = File.ReadAllText(Arquivo);
            Atual = JsonSerializer.Deserialize<Config>(texto, Opcoes) ?? new Config();
            return null;
        }
        catch (JsonException ex)
        {
            return $"config.json inválido (linha {ex.LineNumber + 1}): {ex.Message}";
        }
        catch (IOException ex)
        {
            return $"Não foi possível ler o config.json: {ex.Message}";
        }
    }

    public void Dispose() => _observador?.Dispose();
}
