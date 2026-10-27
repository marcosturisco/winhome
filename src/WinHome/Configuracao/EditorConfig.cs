namespace WinHome.Configuracao;

// Operações de edição usadas pela tela de Configurações e pelo menu de contexto dos apps.
public sealed class EditorConfig(ConfigService config)
{
    public ConfigService Servico => config;
    Config C => config.Atual;

    // Disparado depois de mudanças de estrutura (grupos, painéis, apps adicionados/movidos/removidos).
    public event Action? EstruturaMudou;

    public void Alterar(Action<Config> mudanca) => config.Alterar(mudanca);

    void Estrutura(Action<Config> mudanca)
    {
        config.Alterar(mudanca, imediato: true);
        EstruturaMudou?.Invoke();
    }

    // Grupos de apps na ordem dos painéis (grupos sem painel vão no fim).
    public List<(string Id, string Titulo)> Grupos()
    {
        var lista = C.Paineis.Where(p => C.Grupos.ContainsKey(p.Id)).Select(p => (p.Id, Titulo(p.Id))).ToList();
        lista.AddRange(C.Grupos.Keys.Where(id => lista.All(g => g.Id != id)).Select(id => (id, Titulo(id))));
        return lista;
    }

    public string Titulo(string id) =>
        C.Paineis.FirstOrDefault(p => p.Id == id)?.Titulo is { Length: > 0 } t ? t : id;

    public void MoverApp(string grupo, AppItem app, int delta) => Estrutura(c =>
    {
        if (!c.Grupos.TryGetValue(grupo, out var apps)) return;
        var i = apps.IndexOf(app);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= apps.Count) return;
        (apps[i], apps[j]) = (apps[j], apps[i]);
    });

    public void MoverAppPara(string de, AppItem app, string para) => Estrutura(c =>
    {
        if (de == para || !c.Grupos.TryGetValue(de, out var origem) || !c.Grupos.TryGetValue(para, out var destino)) return;
        if (origem.Remove(app)) destino.Add(app);
    });

    public void RemoverApp(string grupo, AppItem app) => Estrutura(c =>
    {
        if (c.Grupos.TryGetValue(grupo, out var apps)) apps.Remove(app);
    });

    // Adiciona sem repetir o mesmo app/caminho no grupo.
    public void AdicionarApp(string grupo, AppItem app) => Estrutura(c =>
    {
        if (!c.Grupos.TryGetValue(grupo, out var apps)) c.Grupos[grupo] = apps = [];
        bool igual(AppItem a) =>
            (app.App is not null && string.Equals(a.App, app.App, StringComparison.OrdinalIgnoreCase)) ||
            (app.Caminho is not null && string.Equals(a.Caminho, app.Caminho, StringComparison.OrdinalIgnoreCase));
        if (!apps.Any(igual)) apps.Add(app);
    });

    public string NovoGrupo()
    {
        var n = 1;
        while (C.Grupos.ContainsKey($"grupo{n}") || C.Paineis.Any(p => p.Id == $"grupo{n}")) n++;
        var id = $"grupo{n}";
        Estrutura(c =>
        {
            c.Grupos[id] = [];
            c.Paineis.Add(new Painel { Id = id, Titulo = "Novo grupo", Largura = 2 });
        });
        return id;
    }

    public void AdicionarRelogio() => Estrutura(c =>
    {
        if (c.Paineis.All(p => p.Id != "relogio"))
            c.Paineis.Insert(0, new Painel { Id = "relogio", Largura = c.Tema.Colunas });
    });

    public void MoverPainel(Painel painel, int delta) => Estrutura(c =>
    {
        var i = c.Paineis.IndexOf(painel);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= c.Paineis.Count) return;
        (c.Paineis[i], c.Paineis[j]) = (c.Paineis[j], c.Paineis[i]);
    });

    // Remove o painel e, se for um grupo de apps, a lista de apps dele.
    public void RemoverPainel(Painel painel) => Estrutura(c =>
    {
        c.Paineis.Remove(painel);
        c.Grupos.Remove(painel.Id);
    });
}
