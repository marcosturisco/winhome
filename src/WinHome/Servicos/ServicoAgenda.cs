using System.Security.Cryptography;
using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace WinHome.Servicos;

// Um compromisso já calculado para o período (repetições e fusos resolvidos), no horário local.
public sealed record Compromisso(string Titulo, DateTime Inicio, DateTime Fim, bool DiaTodo, string? Local);

// Google Agenda pelo "Endereço secreto no formato iCal" (sem login nem chave). O endereço é secreto: não aparece
// em nome de arquivo (o cache usa um hash), em mensagem de erro nem em log. Atualiza a cada 15 min pelo Rede.
public static class ServicoAgenda
{
    static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    // Compromissos de hoje até "dias" dias à frente (cache em memória e disco; sem rede, o último salvo).
    public static Task<Resultado<List<Compromisso>>?> Obter(string endereco, int dias, bool forcar = false) =>
        Rede.Obter(Chave(endereco, dias), Validade, async () => Calcular(await Baixar(endereco), DateTime.Today, dias), forcar);

    // Botão "Testar" das Configurações: mensagem curta, sem nunca repetir o endereço.
    public static async Task<string> Testar(string endereco)
    {
        if (string.IsNullOrWhiteSpace(endereco)) return "Cole o endereço secreto no formato iCal.";
        if (!Uri.TryCreate(Normalizar(endereco), UriKind.Absolute, out var u) || u.Scheme != "https")
            return "O endereço precisa começar com https:// (ou webcal://).";
        try
        {
            var lista = Calcular(await Baixar(endereco), DateTime.Today, 7);
            return lista.Count == 0
                ? "Agenda lida: nenhum compromisso nos próximos 7 dias."
                : $"Agenda lida: {lista.Count} compromisso(s) nos próximos 7 dias.";
        }
        catch (HttpRequestException ex)
        {
            return ex.StatusCode is { } s ? $"O Google respondeu {(int)s}: confira se o endereço é o secreto (iCal) e está completo." : "Sem conexão com o Google Agenda.";
        }
        catch (TaskCanceledException) { return "O Google Agenda não respondeu em 8 s."; }
        catch (Exception) { return "O conteúdo baixado não é uma agenda iCal válida."; }
    }

    static async Task<string> Baixar(string endereco) => await Rede.Http.GetStringAsync(Normalizar(endereco));

    static string Normalizar(string endereco)
    {
        var e = endereco.Trim();
        return e.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase) ? "https://" + e["webcal://".Length..] : e;
    }

    // Nome do cache: hash do endereço (não dá para recuperar o link a partir dele) + período.
    static string Chave(string endereco, int dias)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalizar(endereco))))[..16];
        return $"agenda_{hash}_{DateTime.Today:yyyyMMdd}_{dias}";
    }

    // Qualquer erro de leitura vira FormatException, que o Rede trata (mantém o último valor salvo).
    static List<Compromisso> Calcular(string ics, DateTime inicio, int dias)
    {
        try { return CalcularSemTratar(ics, inicio, dias); }
        catch (Exception ex) when (ex is not FormatException) { throw new FormatException("iCal inválido", ex); }
    }

    static List<Compromisso> CalcularSemTratar(string ics, DateTime inicio, int dias)
    {
        var calendario = Calendar.Load(ics) ?? throw new FormatException();
        var fim = inicio.AddDays(Math.Clamp(dias, 1, 14));
        var lista = new List<Compromisso>();
        // A sequência é preguiçosa e vem em ordem de início: começa uma semana antes (para pegar o que já estava
        // em andamento, como um evento de vários dias) e para ao passar do fim do período.
        var desde = new CalDateTime(DateTime.SpecifyKind(inicio.AddDays(-7).ToUniversalTime(), DateTimeKind.Utc), "UTC", true);
        foreach (var o in calendario.GetOccurrences(desde, null).Take(5000))
        {
            var diaTodo = !o.Period.StartTime.HasTime;
            var comeco = Local(o.Period.StartTime);
            if (comeco >= fim) break;
            if (o.Source is not CalendarEvent ev) continue;
            if (string.Equals(ev.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)) continue;
            var termino = o.Period.EffectiveEndTime is { } t ? Local(t) : comeco.AddDays(diaTodo ? 1 : 0);
            if (termino <= inicio) continue;
            lista.Add(new Compromisso(string.IsNullOrWhiteSpace(ev.Summary) ? "(sem título)" : ev.Summary.Trim(),
                comeco, termino, diaTodo, string.IsNullOrWhiteSpace(ev.Location) ? null : ev.Location.Trim()));
        }
        return lista.OrderBy(c => c.Inicio.Date).ThenBy(c => !c.DiaTodo).ThenBy(c => c.Inicio).ToList();
    }

    // Dia todo e horário "flutuante" (sem fuso) valem como estão; os demais vêm do UTC para o horário do PC.
    static DateTime Local(CalDateTime d) =>
        !d.HasTime ? d.Value.Date
        : d.IsFloating ? DateTime.SpecifyKind(d.Value, DateTimeKind.Local)
        : DateTime.SpecifyKind(d.AsUtc, DateTimeKind.Utc).ToLocalTime();
}
