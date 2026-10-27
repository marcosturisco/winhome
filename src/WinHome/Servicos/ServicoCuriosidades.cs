using System.Text.Json;

namespace WinHome.Servicos;

// Tipo: "destaque" (curadoria da Wikipédia), "evento", "nascimento" ou "morte".
public sealed record Fato(int Ano, string Texto, string Tipo, string? Imagem, string? Titulo, string? Link);

// "Neste dia" da Wikipédia em português (gratuito, sem chave). Envia só dia e mês; uma busca por dia.
public static class ServicoCuriosidades
{
    static readonly TimeSpan Validade = TimeSpan.FromHours(12);

    static readonly (string Campo, string Tipo)[] Campos =
        [("selected", "destaque"), ("events", "evento"), ("births", "nascimento"), ("deaths", "morte")];

    public static Task<Resultado<List<Fato>>?> Obter(DateTime dia) =>
        Rede.Obter($"curiosidades_{dia:MM_dd}", Validade, async () =>
        {
            var url = $"https://pt.wikipedia.org/api/rest_v1/feed/onthisday/all/{dia:MM}/{dia:dd}";
            using var doc = JsonDocument.Parse(await Rede.Http.GetStringAsync(url));
            var fatos = new List<Fato>();
            var vistos = new HashSet<string>(); // "selected" repete itens de "events"
            foreach (var (campo, tipo) in Campos)
            {
                if (!doc.RootElement.TryGetProperty(campo, out var lista)) continue;
                foreach (var item in lista.EnumerateArray())
                {
                    if (!item.TryGetProperty("year", out var ano) || !item.TryGetProperty("text", out var texto)) continue;
                    var t = texto.GetString();
                    if (string.IsNullOrWhiteSpace(t) || !vistos.Add(t)) continue;

                    string? imagem = null, titulo = null, link = null;
                    if (item.TryGetProperty("pages", out var paginas) && paginas.GetArrayLength() > 0)
                    {
                        var p = paginas[0];
                        if (p.TryGetProperty("thumbnail", out var th) && th.TryGetProperty("source", out var src)) imagem = src.GetString();
                        if (p.TryGetProperty("titles", out var ti) && ti.TryGetProperty("normalized", out var n)) titulo = n.GetString();
                        if (p.TryGetProperty("content_urls", out var cu) && cu.TryGetProperty("desktop", out var d) &&
                            d.TryGetProperty("page", out var pg)) link = pg.GetString();
                    }
                    fatos.Add(new Fato(ano.GetInt32(), t, tipo, imagem, titulo, link));
                }
            }
            return fatos;
        });
}
