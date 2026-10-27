using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Backup e restauração: um .zip com config.json, tarefas.json e videos.json (estado.json fica de fora: posição da
// janela é deste PC). Os endereços secretos da agenda só entram se o usuário pedir; ao restaurar um backup sem eles,
// os deste PC são mantidos (pelo nome da agenda). Antes de restaurar, os arquivos atuais vão para uma pasta de reserva.
public static class ServicoBackup
{
    static readonly string[] Arquivos = ["config.json", "tarefas.json", "videos.json"];
    static readonly JsonSerializerOptions Escrita = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    static readonly JsonDocumentOptions Leitura = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public static string NomeSugerido => $"WinHome-backup-{DateTime.Now:yyyy-MM-dd}";

    // Devolve o erro (ou null).
    public static string? Fazer(string destino, bool incluirSegredos)
    {
        try
        {
            if (File.Exists(destino)) File.Delete(destino);
            using var zip = ZipFile.Open(destino, ZipArchiveMode.Create);
            foreach (var nome in Arquivos)
            {
                var caminho = Path.Combine(ConfigService.Pasta, nome);
                if (!File.Exists(caminho)) continue;
                var texto = File.ReadAllText(caminho);
                if (nome == "config.json" && !incluirSegredos) texto = SemSegredos(texto);
                using var escrita = new StreamWriter(zip.CreateEntry(nome).Open());
                escrita.Write(texto);
            }
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return $"Não foi possível fazer o backup: {ex.Message}";
        }
    }

    // O que o backup contém, para a confirmação (null + erro se não for um backup do WinHome).
    public static (List<string>? Conteudo, string? Erro) Examinar(string origem)
    {
        try
        {
            using var zip = ZipFile.OpenRead(origem);
            var config = zip.GetEntry("config.json");
            if (config is null) return (null, "Este arquivo não é um backup do WinHome (falta o config.json).");
            using (var leitor = new StreamReader(config.Open()))
                JsonSerializer.Deserialize<Config>(leitor.ReadToEnd(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
            return (Arquivos.Where(a => zip.GetEntry(a) is not null).ToList(), null);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        {
            return (null, $"Não foi possível ler o backup: {ex.Message}");
        }
    }

    // Guarda os atuais em "antes-da-restauracao-<data>" e grava os do backup. Devolve (pasta de reserva, erro).
    public static (string? Reserva, string? Erro) Restaurar(string origem)
    {
        try
        {
            var reserva = Path.Combine(ConfigService.Pasta, $"antes-da-restauracao-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
            Directory.CreateDirectory(reserva);
            foreach (var nome in Arquivos)
            {
                var atual = Path.Combine(ConfigService.Pasta, nome);
                if (File.Exists(atual)) File.Copy(atual, Path.Combine(reserva, nome), overwrite: true);
            }

            using var zip = ZipFile.OpenRead(origem);
            foreach (var nome in Arquivos)
            {
                if (zip.GetEntry(nome) is not { } entrada) continue;
                using var leitor = new StreamReader(entrada.Open());
                var texto = leitor.ReadToEnd();
                var destino = Path.Combine(ConfigService.Pasta, nome);
                if (nome == "config.json" && File.Exists(destino)) texto = ManterSegredos(texto, File.ReadAllText(destino));
                File.WriteAllText(destino + ".tmp", texto);
                File.Move(destino + ".tmp", destino, overwrite: true);
            }
            ServicoTarefas.Esquecer();
            EstadoVideos.Esquecer();
            return (reserva, null);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return (null, $"Não foi possível restaurar: {ex.Message}");
        }
    }

    // Tira o "endereco" de cada agenda (o resto da config fica igual; comentários // do arquivo se perdem).
    static string SemSegredos(string config)
    {
        var raiz = JsonNode.Parse(config, documentOptions: Leitura);
        foreach (var cal in Calendarios(raiz)) cal["endereco"] = "";
        return raiz?.ToJsonString(Escrita) ?? config;
    }

    // Agendas do backup sem endereço recebem o deste PC, quando há uma agenda com o mesmo nome.
    static string ManterSegredos(string doBackup, string atual)
    {
        JsonNode? raiz;
        Dictionary<string, string> enderecos;
        try
        {
            raiz = JsonNode.Parse(doBackup, documentOptions: Leitura);
            enderecos = Calendarios(JsonNode.Parse(atual, documentOptions: Leitura))
                .Where(c => c["endereco"]?.GetValue<string>() is { Length: > 0 })
                .GroupBy(c => c["nome"]?.GetValue<string>() ?? "")
                .ToDictionary(g => g.Key, g => g.First()["endereco"]!.GetValue<string>());
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return doBackup; } // config atual inválida: restaura o backup como veio
        var mudou = false;
        foreach (var cal in Calendarios(raiz))
            if (string.IsNullOrEmpty(cal["endereco"]?.GetValue<string>()) &&
                enderecos.TryGetValue(cal["nome"]?.GetValue<string>() ?? "", out var e))
            {
                cal["endereco"] = e;
                mudou = true;
            }
        return mudou ? raiz!.ToJsonString(Escrita) : doBackup;
    }

    static IEnumerable<JsonObject> Calendarios(JsonNode? raiz) =>
        (raiz?["agenda"]?["calendarios"] as JsonArray)?.OfType<JsonObject>() ?? [];
}
