using System.Globalization;
using System.Text.Json;

namespace WinHome.Servicos;

// Variacao = % do dia; null quando a API manda um valor implausível (ver Plausivel).
public sealed record Cotacao(string Moeda, double Valor, double? Variacao, DateTime Horario);

// Cotações em reais pela AwesomeAPI (gratuita, sem chave). Envia só os códigos das moedas.
public static class ServicoCotacoes
{
    static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // Moedas oferecidas na tela de Configurações (todas têm par com o real na AwesomeAPI).
    public static readonly (string Codigo, string Nome)[] Conhecidas =
    [
        ("USD", "Dólar americano"), ("EUR", "Euro"), ("GBP", "Libra esterlina"), ("JPY", "Iene japonês"),
        ("CAD", "Dólar canadense"), ("AUD", "Dólar australiano"), ("CHF", "Franco suíço"), ("CNY", "Yuan chinês"),
        ("ARS", "Peso argentino"), ("BTC", "Bitcoin"), ("ETH", "Ethereum"),
    ];

    // Trava de bom senso: moeda comum não varia mais de 5% num dia; acima disso a AwesomeAPI comparou com uma
    // referência errada (já mandou ~8% para o dólar). Criptomoedas variam muito de verdade e ficam de fora.
    static readonly string[] Cripto = ["BTC", "ETH"];
    const double LimiteDiario = 5;

    static bool Plausivel(string moeda, double pct) => Cripto.Contains(moeda) || Math.Abs(pct) <= LimiteDiario;

    public static string Nome(string codigo) =>
        Conhecidas.FirstOrDefault(m => m.Codigo == codigo).Nome ?? codigo;

    public static Task<Resultado<List<Cotacao>>?> Obter(IReadOnlyList<string> moedas)
    {
        var codigos = moedas.Select(m => m.Trim().ToUpperInvariant()).Where(m => m.Length > 0).Distinct().ToList();
        return Rede.Obter($"cotacoes_{string.Join('-', codigos)}", Validade, async () =>
        {
            var pares = string.Join(',', codigos.Select(m => $"{m}-BRL"));
            using var doc = JsonDocument.Parse(await Rede.Http.GetStringAsync($"https://economia.awesomeapi.com.br/json/last/{pares}"));
            var lista = new List<Cotacao>();
            foreach (var m in codigos)
            {
                if (!doc.RootElement.TryGetProperty($"{m}BRL", out var c)) continue;
                var pct = double.Parse(c.GetProperty("pctChange").GetString()!, Inv);
                lista.Add(new Cotacao(
                    m,
                    double.Parse(c.GetProperty("bid").GetString()!, Inv),
                    Plausivel(m, pct) ? pct : null,
                    DateTime.ParseExact(c.GetProperty("create_date").GetString()!, "yyyy-MM-dd HH:mm:ss", Inv)));
            }
            return lista;
        });
    }
}
