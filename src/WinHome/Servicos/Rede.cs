using System.Text.Json;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Valor vindo da rede ou do cache. Atual = false quando a busca falhou e sobrou só o último valor salvo.
public sealed record Resultado<T>(T Valor, DateTime Quando, bool Atual);

// Acesso à internet com tempo limite e cache em memória + disco (%APPDATA%\WinHome\cache).
// Usado só da thread da interface; falhas de rede nunca viram exceção para quem chama.
public static class Rede
{
    public static readonly HttpClient Http = Criar();

    static HttpClient Criar()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WinHome/0.3 (+https://github.com/marcosturisco/winhome)");
        return http;
    }

    static readonly string Pasta = Path.Combine(ConfigService.Pasta, "cache");
    static readonly Dictionary<string, (DateTime Quando, object Valor)> Memoria = [];
    static readonly Dictionary<string, Task> EmAndamento = [];

    sealed record Arquivo<T>(DateTime Quando, T Valor);

    // Devolve o valor do cache se ainda vale; senão busca. Sem rede, devolve o último valor salvo (ou null).
    public static async Task<Resultado<T>?> Obter<T>(string chave, TimeSpan validade, Func<Task<T>> buscar) where T : class
    {
        if (Memoria.TryGetValue(chave, out var m) && DateTime.Now - m.Quando < validade)
            return new((T)m.Valor, m.Quando, true);

        var salvo = Memoria.TryGetValue(chave, out m) ? new Arquivo<T>(m.Quando, (T)m.Valor) : await LerDisco<T>(chave);
        if (salvo is not null && DateTime.Now - salvo.Quando < validade)
        {
            Memoria[chave] = (salvo.Quando, salvo.Valor);
            return new(salvo.Valor, salvo.Quando, true);
        }

        // Várias telas pedindo a mesma coisa ao mesmo tempo compartilham uma busca só.
        if (!EmAndamento.TryGetValue(chave, out var tarefa))
        {
            tarefa = Buscar(chave, buscar);
            EmAndamento[chave] = tarefa;
        }
        var novo = await (Task<T?>)tarefa;
        EmAndamento.Remove(chave);

        if (novo is not null) return new(novo, Memoria[chave].Quando, true);
        return salvo is null ? null : new(salvo.Valor, salvo.Quando, false);
    }

    static async Task<T?> Buscar<T>(string chave, Func<Task<T>> buscar) where T : class
    {
        try
        {
            var valor = await buscar();
            var agora = DateTime.Now;
            Memoria[chave] = (agora, valor);
            await GravarDisco(chave, new Arquivo<T>(agora, valor));
            return valor;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    static string Caminho(string chave) =>
        Path.Combine(Pasta, string.Concat(chave.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')) + ".json");

    static async Task<Arquivo<T>?> LerDisco<T>(string chave)
    {
        try
        {
            var arquivo = Caminho(chave);
            if (!File.Exists(arquivo)) return null;
            return JsonSerializer.Deserialize<Arquivo<T>>(await File.ReadAllTextAsync(arquivo));
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    static async Task GravarDisco<T>(string chave, Arquivo<T> dados)
    {
        try
        {
            Directory.CreateDirectory(Pasta);
            await File.WriteAllTextAsync(Caminho(chave), JsonSerializer.Serialize(dados));
        }
        catch (IOException)
        {
        }
    }
}
