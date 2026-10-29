using Microsoft.UI.Dispatching;
using Windows.Devices.Enumeration.Pnp;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Um aparelho com bateria. Nome = como o Windows mostra (chave de "ocultos" e "apelidos"); Categoria = ex.:
// "Input.Mouse", "Input.Keyboard", "Audio.Earbuds"; Leitura = quando o aparelho informou o nível pela última vez.
public sealed record Bateria(string Nome, int Nivel, bool Conectado, string Categoria, DateTimeOffset? Leitura);

// Lê do Windows a bateria dos aparelhos Bluetooth — a mesma informação de Configurações → Bluetooth e dispositivos —
// a cada minuto, enquanto o WinHome roda (o aviso precisa vir mesmo com a Home escondida). Só leitura, sem rede.
// Um aparelho aparece no Windows como várias entradas (ex.: fone = áudio, microfone, cada lado do fone sem fio);
// elas são agrupadas pelo "container": conexão e nome vêm da entrada principal (DEV_…), o nível é o menor do grupo.
public static class ServicoBaterias
{
    const string ChaveNivel = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    const string ChaveLeitura = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 7";
    const string ChaveConectado = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";
    const string ChaveContainer = "System.Devices.ContainerId";
    const string ChaveNome = "System.ItemNameDisplay";
    const string ChaveCategorias = "System.Devices.CategoryIds";
    static readonly string[] Propriedades = [ChaveNivel, ChaveLeitura, ChaveConectado, ChaveContainer, ChaveNome, ChaveCategorias];
    const string Filtro = "System.Devices.DeviceInstanceId:~<\"BTH\""; // BTHENUM (Bluetooth clássico) e BTHLE

    // Aparelhos já avisados nesta descarga; sai da lista quando o nível volta a "baixa" + 5 (foi carregado).
    static readonly HashSet<string> Avisados = [];
    static DispatcherQueueTimer? _relogio;
    static Func<Baterias>? _config;
    static bool _lendo;

    // Conectados primeiro; dentro de cada grupo, o mais descarregado em cima.
    public static IReadOnlyList<Bateria> Atuais { get; private set; } = [];

    public static event Action? Mudou;                 // na thread da interface
    public static event Action<Bateria>? FicouBaixa;   // uma vez por descarga, só para aparelhos conectados e visíveis

    public static void Iniciar(Func<Baterias> config)
    {
        _config = config;
        if (_relogio is not null) return;
        _relogio = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _relogio.Interval = TimeSpan.FromMinutes(1);
        _relogio.Tick += (_, _) => _ = Atualizar();
        _relogio.Start();
        _ = Atualizar();
    }

    public static async Task Atualizar()
    {
        if (_lendo) return;
        _lendo = true;
        try { Atuais = await Ler(); }
        catch (Exception) { } // falha na leitura: fica a lista anterior
        finally { _lendo = false; }
        Verificar();
        Mudou?.Invoke();
    }

    public enum Faixa { Boa, Media, Baixa }

    public static Faixa FaixaDe(int nivel, Baterias cfg) =>
        nivel < cfg.Baixa ? Faixa.Baixa : nivel < Math.Max(cfg.Media, cfg.Baixa) ? Faixa.Media : Faixa.Boa;

    static async Task<IReadOnlyList<Bateria>> Ler()
    {
        var objetos = await PnpObject.FindAllAsync(PnpObjectType.Device, Propriedades, Filtro);
        return objetos
            .Where(o => Valor(o, ChaveContainer) is Guid)
            .GroupBy(o => (Guid)Valor(o, ChaveContainer)!)
            .Select(g => Montar(g.ToList()))
            .OfType<Bateria>()
            .OrderByDescending(b => b.Conectado)
            .ThenBy(b => b.Nivel)
            .ToList();
    }

    static Bateria? Montar(List<PnpObject> entradas)
    {
        var niveis = entradas.Select(o => Valor(o, ChaveNivel))
            .Where(v => v is byte or sbyte or short or ushort or int or uint)
            .Select(v => Math.Clamp(Convert.ToInt32(v), 0, 100)).ToList();
        if (niveis.Count == 0) return null;
        var principais = entradas.Where(o => o.Id.Contains(@"\DEV_", StringComparison.OrdinalIgnoreCase)).ToList();
        if (principais.Count == 0) principais = entradas;

        var nome = principais.Concat(entradas).Select(o => (Valor(o, ChaveNome) as string)?.Trim())
            .FirstOrDefault(n => !string.IsNullOrEmpty(n));
        if (nome is null) return null;
        var categoria = principais.SelectMany(o => Valor(o, ChaveCategorias) as string[] ?? [])
            .FirstOrDefault(c => c != "Unknown") ?? "";
        var leitura = entradas.Select(o => Valor(o, ChaveLeitura)).OfType<DateTimeOffset>()
            .Select(d => (DateTimeOffset?)d).Max();
        var conectado = principais.Any(o => Valor(o, ChaveConectado) is true);
        return new Bateria(nome, niveis.Min(), conectado, categoria, leitura);
    }

    static object? Valor(PnpObject o, string chave) => o.Properties.TryGetValue(chave, out var v) ? v : null;

    static void Verificar()
    {
        if (_config is null) return;
        var cfg = _config();
        foreach (var b in Atuais)
        {
            if (b.Nivel >= cfg.Baixa + 5) Avisados.Remove(b.Nome);
            else if (b.Conectado && b.Nivel < cfg.Baixa && cfg.Visivel(b.Nome) && Avisados.Add(b.Nome))
                FicouBaixa?.Invoke(b);
        }
    }
}
