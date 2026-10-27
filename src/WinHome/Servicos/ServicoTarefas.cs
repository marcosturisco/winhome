using System.Text.Encodings.Web;
using System.Text.Json;
using WinHome.Configuracao;

namespace WinHome.Servicos;

public sealed class Tarefa
{
    public string Texto { get; set; } = "";
    public bool Feita { get; set; }
    public DateTime Criada { get; set; } = DateTime.Now;
    public DateTime? Concluida { get; set; }
}

// Lista de tarefas em %APPDATA%\WinHome\tarefas.json, separada da config (o editor da config não a regrava).
// Fica em memória durante a sessão: a Home é remontada a cada mudança de config sem reler o disco.
public static class ServicoTarefas
{
    public static readonly string Arquivo = Path.Combine(ConfigService.Pasta, "tarefas.json");

    static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static List<Tarefa>? _lista;

    // Erro da leitura (arquivo inválido) ou da última gravação, para o painel mostrar.
    public static string? Erro { get; private set; }

    public static List<Tarefa> Lista => _lista ??= Ler();

    // O arquivo foi trocado por fora (restauração de backup): lê de novo na próxima vez.
    public static void Esquecer()
    {
        _lista = null;
        Erro = null;
    }

    static List<Tarefa> Ler()
    {
        try
        {
            if (!File.Exists(Arquivo)) return [];
            return JsonSerializer.Deserialize<List<Tarefa>>(File.ReadAllText(Arquivo), Opcoes) ?? [];
        }
        catch (JsonException)
        {
            // Guarda o arquivo estragado antes que a próxima gravação o substitua.
            var copia = Arquivo + ".erro";
            try { File.Copy(Arquivo, copia, overwrite: true); } catch (IOException) { }
            Erro = $"tarefas.json inválido; começando uma lista nova (o arquivo antigo ficou em {Path.GetFileName(copia)}).";
            return [];
        }
        catch (IOException ex)
        {
            Erro = $"Não foi possível ler o tarefas.json: {ex.Message}";
            return [];
        }
    }

    public static void Adicionar(string texto)
    {
        texto = texto.Trim();
        if (texto.Length == 0) return;
        // Nova tarefa entra depois da última pendente (as concluídas ficam no fim).
        var i = Lista.FindLastIndex(t => !t.Feita) + 1;
        Lista.Insert(i, new Tarefa { Texto = texto });
        Gravar();
    }

    public static void Marcar(Tarefa tarefa, bool feita)
    {
        if (tarefa.Feita == feita) return;
        tarefa.Feita = feita;
        tarefa.Concluida = feita ? DateTime.Now : null;
        Lista.Remove(tarefa);
        // Concluída vai para o começo das concluídas; reaberta, para o fim das pendentes.
        Lista.Insert(Lista.FindLastIndex(t => !t.Feita) + 1, tarefa);
        Gravar();
    }

    public static void Renomear(Tarefa tarefa, string texto)
    {
        texto = texto.Trim();
        if (texto.Length == 0 || texto == tarefa.Texto) return;
        tarefa.Texto = texto;
        Gravar();
    }

    // Move dentro do mesmo grupo (pendentes ou concluídas).
    public static void Mover(Tarefa tarefa, int delta)
    {
        var i = Lista.IndexOf(tarefa);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= Lista.Count || Lista[j].Feita != tarefa.Feita) return;
        (Lista[i], Lista[j]) = (Lista[j], Lista[i]);
        Gravar();
    }

    public static void Remover(Tarefa tarefa)
    {
        if (Lista.Remove(tarefa)) Gravar();
    }

    public static void LimparConcluidas()
    {
        if (Lista.RemoveAll(t => t.Feita) > 0) Gravar();
    }

    // Grava num arquivo temporário e troca: uma falha no meio não deixa o tarefas.json pela metade.
    static void Gravar()
    {
        try
        {
            Directory.CreateDirectory(ConfigService.Pasta);
            var temporario = Arquivo + ".tmp";
            File.WriteAllText(temporario, JsonSerializer.Serialize(Lista, Opcoes));
            File.Move(temporario, Arquivo, overwrite: true);
            Erro = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Erro = $"Não foi possível salvar o tarefas.json: {ex.Message}";
        }
    }
}
