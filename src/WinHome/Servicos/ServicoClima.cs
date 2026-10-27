using System.Globalization;
using System.Text.Json;
using WinHome.Configuracao;

namespace WinHome.Servicos;

public sealed record Previsao(double Temperatura, double Maxima, double Minima, int Codigo, bool Dia);

// Clima pelo Open-Meteo (gratuito, sem chave). Envia só coordenadas/nome da cidade.
public static class ServicoClima
{
    static readonly TimeSpan Validade = TimeSpan.FromMinutes(30);
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static Task<Resultado<Previsao>?> Obter(Cidade cidade, string unidade)
    {
        var f = unidade == "F";
        var chave = $"clima_{cidade.Latitude.ToString("F3", Inv)}_{cidade.Longitude.ToString("F3", Inv)}_{(f ? "F" : "C")}";
        return Rede.Obter(chave, Validade, async () =>
        {
            var url = "https://api.open-meteo.com/v1/forecast" +
                      $"?latitude={cidade.Latitude.ToString(Inv)}&longitude={cidade.Longitude.ToString(Inv)}" +
                      "&current=temperature_2m,weather_code,is_day&daily=temperature_2m_max,temperature_2m_min" +
                      "&timezone=auto&forecast_days=1" + (f ? "&temperature_unit=fahrenheit" : "");
            using var doc = JsonDocument.Parse(await Rede.Http.GetStringAsync(url));
            var atual = doc.RootElement.GetProperty("current");
            var diario = doc.RootElement.GetProperty("daily");
            return new Previsao(
                atual.GetProperty("temperature_2m").GetDouble(),
                diario.GetProperty("temperature_2m_max")[0].GetDouble(),
                diario.GetProperty("temperature_2m_min")[0].GetDouble(),
                atual.GetProperty("weather_code").GetInt32(),
                atual.GetProperty("is_day").GetInt32() == 1);
        });
    }

    // Busca de cidades por nome (para adicionar nas Configurações). Sem rede: lista vazia.
    public static async Task<List<Cidade>> BuscarCidades(string nome)
    {
        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(nome)}&count=8&language=pt&format=json";
            using var doc = JsonDocument.Parse(await Rede.Http.GetStringAsync(url));
            if (!doc.RootElement.TryGetProperty("results", out var lista)) return [];
            return lista.EnumerateArray().Select(r => new Cidade
            {
                Nome = r.GetProperty("name").GetString() ?? "",
                Regiao = r.TryGetProperty("admin1", out var a) ? a.GetString() : null,
                Pais = r.TryGetProperty("country", out var p) ? p.GetString() : null,
                Latitude = r.GetProperty("latitude").GetDouble(),
                Longitude = r.GetProperty("longitude").GetDouble(),
                Fuso = r.TryGetProperty("timezone", out var t) ? t.GetString() : null,
            }).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            return [];
        }
    }

    public static string Descrever(Cidade c) =>
        string.Join(", ", new[] { c.Nome, c.Regiao, c.Pais }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());

    // Códigos WMO do Open-Meteo → ícone (emoji colorido) e descrição.
    public static (string Icone, string Texto) Tempo(int codigo, bool dia) => codigo switch
    {
        0 => (dia ? "☀️" : "🌙", "Céu limpo"),
        1 => (dia ? "🌤️" : "🌙", "Poucas nuvens"),
        2 => ("⛅", "Parcialmente nublado"),
        3 => ("☁️", "Nublado"),
        45 or 48 => ("🌫️", "Neblina"),
        51 or 53 or 55 => ("🌦️", "Garoa"),
        56 or 57 => ("🌧️", "Garoa congelante"),
        61 => ("🌧️", "Chuva fraca"),
        63 => ("🌧️", "Chuva"),
        65 => ("🌧️", "Chuva forte"),
        66 or 67 => ("🌧️", "Chuva congelante"),
        71 or 73 or 75 or 77 => ("❄️", "Neve"),
        80 or 81 => ("🌦️", "Pancadas de chuva"),
        82 => ("⛈️", "Pancadas fortes"),
        85 or 86 => ("🌨️", "Pancadas de neve"),
        95 => ("⛈️", "Trovoada"),
        96 or 99 => ("⛈️", "Trovoada com granizo"),
        _ => ("🌡️", ""),
    };
}
