using System.Text.Encodings.Web;
using System.Text.Json;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Volume e últimos vídeos abertos no painel Vídeos: %APPDATA%\WinHome\videos.json, fora da config
// (mudam o tempo todo e não devem remontar a Home a cada ajuste de volume).
public sealed class EstadoVideos
{
    public int Volume { get; set; } = 80;
    public List<string> Recentes { get; set; } = [];

    const int MaximoRecentes = 12;
    static readonly string Arquivo = Path.Combine(ConfigService.Pasta, "videos.json");
    static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static EstadoVideos? _atual;
    public static EstadoVideos Atual => _atual ??= Ler();

    static EstadoVideos Ler()
    {
        try
        {
            return File.Exists(Arquivo) ? JsonSerializer.Deserialize<EstadoVideos>(File.ReadAllText(Arquivo), Opcoes) ?? new() : new();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return new();
        }
    }

    public void Lembrar(string fonte)
    {
        Recentes.RemoveAll(r => string.Equals(r, fonte, StringComparison.OrdinalIgnoreCase));
        Recentes.Insert(0, fonte);
        if (Recentes.Count > MaximoRecentes) Recentes.RemoveRange(MaximoRecentes, Recentes.Count - MaximoRecentes);
        Gravar();
    }

    public void LimparRecentes()
    {
        Recentes.Clear();
        Gravar();
    }

    public void Gravar()
    {
        try
        {
            var temporario = Arquivo + ".tmp";
            File.WriteAllText(temporario, JsonSerializer.Serialize(this, Opcoes));
            File.Move(temporario, Arquivo, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
