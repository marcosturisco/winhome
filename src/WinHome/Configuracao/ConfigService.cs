using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Dispatching;

namespace WinHome.Configuracao;

// Carrega %APPDATA%\WinHome\config.json (criando a partir do padrão), salva as mudanças feitas pela
// interface e avisa quando o arquivo muda.
public sealed class ConfigService : IDisposable
{
    public static readonly string Pasta =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinHome");
    public static readonly string Arquivo = Path.Combine(Pasta, "config.json");
    static readonly string Backup = Arquivo + ".bak";
    static readonly string Padrao = Path.Combine(AppContext.BaseDirectory, "Assets", "config.padrao.json");

    static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // mantém acentos legíveis no arquivo
    };

    readonly DispatcherQueue _fila;
    readonly DispatcherQueueTimer _espera;
    readonly DispatcherQueueTimer _gravacao;
    FileSystemWatcher? _observador;
    string? _ultimoGravado;

    public Config Atual { get; private set; } = new();

    // Chamado na thread da interface com a config atual e o erro (se houver).
    // doArquivo = true quando veio de uma edição externa do config.json (Atual é um objeto novo).
    public event Action<Config, string?, bool>? Mudou;

    public ConfigService(DispatcherQueue fila)
    {
        _fila = fila;
        // Editores salvam em várias etapas; espera o arquivo assentar antes de reler.
        _espera = fila.CreateTimer();
        _espera.Interval = TimeSpan.FromMilliseconds(300);
        _espera.IsRepeating = false;
        _espera.Tick += (_, _) => Recarregar();

        // Junta mudanças rápidas da interface (digitação, controles deslizantes) numa gravação só.
        _gravacao = fila.CreateTimer();
        _gravacao.Interval = TimeSpan.FromMilliseconds(250);
        _gravacao.IsRepeating = false;
        _gravacao.Tick += (_, _) => Gravar();
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

    // Aplica uma mudança feita pela interface; imediato = grava e reaplica agora (mudanças de estrutura).
    public void Alterar(Action<Config> mudanca, bool imediato = false)
    {
        mudanca(Atual);
        _gravacao.Stop();
        if (imediato) Gravar();
        else _gravacao.Start();
    }

    void Gravar()
    {
        _gravacao.Stop();
        string? erro = null;
        try
        {
            var texto = JsonSerializer.Serialize(Atual, Opcoes);
            if (File.Exists(Arquivo)) File.Copy(Arquivo, Backup, overwrite: true);
            _ultimoGravado = texto;
            File.WriteAllText(Arquivo, texto);
        }
        catch (IOException ex)
        {
            erro = $"Não foi possível salvar o config.json: {ex.Message}";
        }
        Mudou?.Invoke(Atual, erro, false);
    }

    void Agendar() => _fila.TryEnqueue(() => { _espera.Stop(); _espera.Start(); });

    // Relê o config.json agora (depois de restaurar um backup), mesmo sem esperar o observador do arquivo.
    public void RecarregarAgora()
    {
        _espera.Stop();
        _ultimoGravado = null;
        Recarregar();
    }

    void Recarregar()
    {
        string texto;
        try { texto = File.ReadAllText(Arquivo); }
        catch (IOException) { return; } // ainda sendo gravado; o próximo aviso do observador relê
        if (texto == _ultimoGravado) return; // é a nossa própria gravação
        Mudou?.Invoke(Atual, Ler(), true);
    }

    string? Ler()
    {
        try
        {
            if (!File.Exists(Arquivo)) return null;
            var texto = File.ReadAllText(Arquivo);
            var nova = JsonSerializer.Deserialize<Config>(texto, Opcoes) ?? new Config();
            nova.Migrar();
            Atual = nova;
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

    public void Dispose()
    {
        _observador?.Dispose();
        if (_gravacao.IsRunning) // mudança ainda não gravada ao sair
        {
            _gravacao.Stop();
            try { File.WriteAllText(Arquivo, JsonSerializer.Serialize(Atual, Opcoes)); }
            catch (IOException) { }
        }
    }
}
