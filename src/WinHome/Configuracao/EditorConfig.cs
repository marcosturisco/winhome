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

    // Painéis especiais (não são grupos de apps): no máximo um de cada.
    public static readonly (string Id, string Nome)[] Especiais =
    [
        (Config.PainelBoasVindas, "Boas-vindas (saudação, hora e data)"),
        (Config.PainelMensagem, "Mensagem do dia"),
        (Config.PainelCuriosidade, "Curiosidades do dia"),
        (Config.PainelProjetos, "Projetos recentes"),
        (Config.PainelTarefas, "Tarefas"),
        (Config.PainelAcoes, "Ações rápidas"),
        (Config.PainelVideos, "Vídeos (VLC e YouTube)"),
    ];

    public bool Tem(string id) => C.Paineis.Any(p => p.Id == id);

    public bool Exibido(string id) => C.Paineis.Any(p => p.Id == id && p.Visivel);

    // Toggle de exibição de um painel especial: ligar adiciona o painel (se ainda não existe) ou o reexibe.
    public void Exibir(string id, bool exibir)
    {
        if (exibir && !Tem(id))
        {
            AdicionarEspecial(id);
            return;
        }
        Estrutura(c =>
        {
            foreach (var p in c.Paineis.Where(p => p.Id == id)) p.Visivel = exibir;
        });
    }

    public void AdicionarEspecial(string id) => Estrutura(c =>
    {
        if (c.Paineis.Any(p => p.Id == id)) return;
        if (id == Config.PainelBoasVindas)
            c.Paineis.Insert(0, new Painel { Id = id, Largura = c.Tema.Colunas }); // topo, largura total
        else if (id == Config.PainelMensagem)
            c.Paineis.Add(new Painel { Id = id, Titulo = "Mensagem do dia", Largura = Math.Max(1, c.Tema.Colunas / 2) });
        else if (id == Config.PainelCuriosidade)
            c.Paineis.Add(new Painel { Id = id, Titulo = "Curiosidades do dia", Largura = Math.Max(1, c.Tema.Colunas / 2) });
        else if (id == Config.PainelProjetos)
            c.Paineis.Add(new Painel { Id = id, Titulo = "Projetos recentes", Largura = Math.Max(1, c.Tema.Colunas / 2) });
        else if (id == Config.PainelTarefas)
            c.Paineis.Add(new Painel { Id = id, Titulo = "Tarefas", Largura = Math.Max(1, c.Tema.Colunas / 2) });
        else if (id == Config.PainelAcoes)
            c.Paineis.Add(new Painel { Id = id, Titulo = "Ações rápidas", Largura = Math.Max(1, c.Tema.Colunas / 2) });
        else if (id == Config.PainelVideos)
            c.Paineis.Add(new Painel { Id = id, Titulo = "Vídeos", Largura = Math.Max(1, c.Tema.Colunas / 2) });
    });

    public void AdicionarCidade(Cidade cidade) => Estrutura(c =>
    {
        var lista = c.BoasVindas.Clima.Cidades;
        if (!lista.Any(x => Math.Abs(x.Latitude - cidade.Latitude) < 0.01 && Math.Abs(x.Longitude - cidade.Longitude) < 0.01))
            lista.Add(cidade);
    });

    public void RemoverCidade(Cidade cidade) => Estrutura(c => c.BoasVindas.Clima.Cidades.Remove(cidade));

    public void MoverCidade(Cidade cidade, int delta) => Estrutura(c => Trocar(c.BoasVindas.Clima.Cidades, cidade, delta));

    public void AdicionarMoeda(string codigo) => Estrutura(c =>
    {
        if (!c.BoasVindas.Cotacoes.Moedas.Contains(codigo)) c.BoasVindas.Cotacoes.Moedas.Add(codigo);
    });

    public void RemoverMoeda(string codigo) => Estrutura(c => c.BoasVindas.Cotacoes.Moedas.Remove(codigo));

    public void MoverMoeda(string codigo, int delta) => Estrutura(c => Trocar(c.BoasVindas.Cotacoes.Moedas, codigo, delta));

    static void Trocar<T>(List<T> lista, T item, int delta)
    {
        var i = lista.IndexOf(item);
        var j = i + delta;
        if (i < 0 || j < 0 || j >= lista.Count) return;
        (lista[i], lista[j]) = (lista[j], lista[i]);
    }

    // Fixados e ocultos guardam o caminho; a comparação ignora maiúsculas e barras (ServicoProjetos.Chave).
    public void FixarProjeto(string caminho, bool fixar) => Estrutura(c =>
    {
        var chave = Servicos.ServicoProjetos.Chave(caminho);
        c.Projetos.Fixados.RemoveAll(f => Servicos.ServicoProjetos.Chave(f) == chave);
        if (fixar) c.Projetos.Fixados.Add(caminho);
    });

    public void OcultarProjeto(string caminho, bool ocultar) => Estrutura(c =>
    {
        var chave = Servicos.ServicoProjetos.Chave(caminho);
        c.Projetos.Ocultos.RemoveAll(f => Servicos.ServicoProjetos.Chave(f) == chave);
        if (ocultar) c.Projetos.Ocultos.Add(caminho);
    });

    public void AdicionarPastaProjetos(string pasta) => Estrutura(c =>
    {
        if (!c.Projetos.Pastas.Any(p => string.Equals(p, pasta, StringComparison.OrdinalIgnoreCase))) c.Projetos.Pastas.Add(pasta);
    });

    public void RemoverPastaProjetos(string pasta) => Estrutura(c => c.Projetos.Pastas.Remove(pasta));

    // Troca no mesmo lugar da lista; se a nova já estiver lá, só tira a antiga.
    public void TrocarPastaProjetos(string antiga, string nova) => Estrutura(c =>
    {
        var pastas = c.Projetos.Pastas;
        var i = pastas.IndexOf(antiga);
        if (i < 0) return;
        if (pastas.Any(p => string.Equals(p, nova, StringComparison.OrdinalIgnoreCase))) pastas.RemoveAt(i);
        else pastas[i] = nova;
    });

    public void NovaAcao() => Estrutura(c => c.AcoesRapidas.Add(new AcaoRapida { Nome = "Nova ação" }));

    public void RemoverAcao(AcaoRapida acao) => Estrutura(c => c.AcoesRapidas.Remove(acao));

    public void MoverAcao(AcaoRapida acao, int delta) => Estrutura(c => Trocar(c.AcoesRapidas, acao, delta));

    public void AdicionarPasso(AcaoRapida acao, PassoAcao passo) => Estrutura(_ => acao.Passos.Add(passo));

    public void RemoverPasso(AcaoRapida acao, PassoAcao passo) => Estrutura(_ => acao.Passos.Remove(passo));

    public void MoverPasso(AcaoRapida acao, PassoAcao passo, int delta) => Estrutura(_ => Trocar(acao.Passos, passo, delta));

    public void RestaurarAcoesPadrao() => Estrutura(c => c.AcoesRapidas = AcaoRapida.Padrao());

    // Ações padrão que ainda não estão na lista (pelo nome); as minhas ficam como estão.
    public List<string> AcoesPadraoQueFaltam() =>
        AcaoRapida.Padrao().Select(a => a.Nome).Where(n => C.AcoesRapidas.All(a => a.Nome != n)).ToList();

    public void AdicionarAcoesQueFaltam() => Estrutura(c =>
    {
        foreach (var padrao in AcaoRapida.Padrao().Where(p => c.AcoesRapidas.All(a => a.Nome != p.Nome)))
            c.AcoesRapidas.Add(padrao);
    });

    public void AdicionarFrase() => Estrutura(c => c.Mensagens.Minhas.Add(new Frase()));

    public void RemoverFrase(Frase frase) => Estrutura(c => c.Mensagens.Minhas.Remove(frase));

    public void DefinirVisivel(Painel painel, bool visivel) => Estrutura(_ => painel.Visivel = visivel);

    public void DefinirLargura(Painel painel, int largura) => Estrutura(c => painel.Largura = Math.Clamp(largura, 1, c.Tema.Colunas));

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
