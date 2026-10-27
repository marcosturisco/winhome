using System.Text.Json;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Onde a Home estava ao fechar: monitor (um ponto dentro dele), modo janela e o retângulo desse modo.
// Fica em %APPDATA%\WinHome\estado.json, fora da config: muda a cada movimento e não deve regravar o config.json.
public sealed class EstadoJanela
{
    public int CentroX { get; set; }
    public int CentroY { get; set; }
    public bool EmJanela { get; set; }
    public int[]? Janela { get; set; } // x, y, largura, altura (pixels da tela)

    static readonly string Arquivo = Path.Combine(ConfigService.Pasta, "estado.json");
    static readonly JsonSerializerOptions Opcoes = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public static EstadoJanela? Ler()
    {
        try
        {
            return File.Exists(Arquivo) ? JsonSerializer.Deserialize<EstadoJanela>(File.ReadAllText(Arquivo), Opcoes) : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null; // sem estado: abre no monitor principal
        }
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
