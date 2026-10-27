using System.Diagnostics;
using System.Xml.Linq;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Origem: "intellij", "androidStudio", "vscode" ou "pasta". Caminho local (D:\...) ou do WSL (\\wsl.localhost\...).
public sealed record Projeto(string Nome, string Caminho, string Origem, DateTime? Quando, string? Branch)
{
    public bool Wsl => Caminho.StartsWith(@"\\wsl", StringComparison.OrdinalIgnoreCase);
}

// Junta os projetos recentes do IntelliJ, Android Studio e VS Code com as subpastas git das pastas configuradas.
// Só lê arquivos locais; nada é alterado.
public static class ServicoProjetos
{
    static readonly string Roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    static readonly string Local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    // A Home é remontada a cada mudança de config; a leitura do disco vale por 30 s.
    static (DateTime Quando, string Assinatura, List<Projeto> Lista)? _cache;

    public static async Task<List<Projeto>> Listar(Projetos cfg)
    {
        var assinatura = string.Join("|", cfg.UsarIntellij, cfg.UsarAndroidStudio, cfg.UsarVsCode, string.Join(";", cfg.Pastas));
        if (_cache is { } c && c.Assinatura == assinatura && DateTime.Now - c.Quando < TimeSpan.FromSeconds(30))
            return c.Lista;
        var lista = await Task.Run(() => Coletar(cfg));
        _cache = (DateTime.Now, assinatura, lista);
        return lista;
    }

    public static void Esquecer() => _cache = null;

    // Chave para juntar o mesmo projeto vindo de fontes diferentes.
    public static string Chave(string caminho) => caminho.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();

    static List<Projeto> Coletar(Projetos cfg)
    {
        var achados = new List<Projeto>();
        if (cfg.UsarIntellij) achados.AddRange(JetBrains(Path.Combine(Roaming, "JetBrains"), "IntelliJIdea", "intellij"));
        if (cfg.UsarAndroidStudio) achados.AddRange(JetBrains(Path.Combine(Roaming, "Google"), "AndroidStudio", "androidStudio"));
        if (cfg.UsarVsCode) achados.AddRange(VsCode());
        foreach (var pasta in cfg.Pastas) achados.AddRange(Pasta(Environment.ExpandEnvironmentVariables(pasta)));

        // Mesmo projeto em várias fontes: fica a data mais recente e a origem dela.
        return achados
            .GroupBy(p => Chave(p.Caminho))
            .Select(g =>
            {
                var maisRecente = g.OrderByDescending(p => p.Quando ?? DateTime.MinValue).First();
                var origem = g.Where(p => p.Origem != "pasta").OrderByDescending(p => p.Quando ?? DateTime.MinValue).FirstOrDefault() ?? maisRecente;
                return maisRecente with { Origem = origem.Origem, Branch = maisRecente.Wsl ? null : Branch(maisRecente.Caminho) };
            })
            .OrderByDescending(p => p.Quando ?? DateTime.MinValue)
            .ToList();
    }

    // recentProjects.xml de todas as versões instaladas (IntelliJIdea2026.1, IntelliJIdea2026.2, ...).
    static IEnumerable<Projeto> JetBrains(string raiz, string prefixo, string origem)
    {
        if (!Directory.Exists(raiz)) yield break;
        foreach (var dir in Directory.EnumerateDirectories(raiz, prefixo + "*"))
        {
            var arquivo = Path.Combine(dir, "options", "recentProjects.xml");
            if (!File.Exists(arquivo)) continue;
            XDocument doc;
            try { doc = XDocument.Load(arquivo); }
            catch (Exception ex) when (ex is IOException or System.Xml.XmlException) { continue; }

            foreach (var entrada in doc.Descendants("entry"))
            {
                var chave = (string?)entrada.Attribute("key");
                var meta = entrada.Descendants("RecentProjectMetaInfo").FirstOrDefault();
                if (chave is null || meta is null) continue;
                var caminho = chave.Replace("$USER_HOME$", Home).Replace('/', '\\');
                if (!caminho.StartsWith(@"\\") && !Directory.Exists(caminho)) continue;

                long ms = 0;
                foreach (var op in meta.Elements("option"))
                    if ((string?)op.Attribute("name") is "projectOpenTimestamp" or "activationTimestamp" &&
                        long.TryParse((string?)op.Attribute("value"), out var v)) ms = Math.Max(ms, v);

                var titulo = ((string?)meta.Attribute("frameTitle"))?.Split(" – ")[0].Trim();
                var nome = string.IsNullOrWhiteSpace(titulo) ? Path.GetFileName(caminho.TrimEnd('\\')) : titulo;
                yield return new Projeto(nome, caminho, origem,
                    ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime : null, null);
            }
        }
    }

    // Cada pasta aberta no VS Code tem uma subpasta em workspaceStorage com workspace.json.
    static IEnumerable<Projeto> VsCode()
    {
        var raiz = Path.Combine(Roaming, "Code", "User", "workspaceStorage");
        if (!Directory.Exists(raiz)) yield break;
        foreach (var dir in new DirectoryInfo(raiz).EnumerateDirectories())
        {
            var arquivo = Path.Combine(dir.FullName, "workspace.json");
            if (!File.Exists(arquivo)) continue;
            string? pasta;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(arquivo));
                pasta = doc.RootElement.TryGetProperty("folder", out var f) ? f.GetString() : null;
            }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException) { continue; }

            var caminho = pasta is null ? null : DeUriVsCode(pasta);
            if (caminho is null) continue;
            if (!caminho.StartsWith(@"\\") && !Directory.Exists(caminho)) continue;
            yield return new Projeto(Path.GetFileName(caminho.TrimEnd('\\')), caminho, "vscode", dir.LastWriteTime, null);
        }
    }

    // file:///d%3A/Documents/x → D:\Documents\x ; vscode-remote://wsl%2Bubuntu/home/x → \\wsl.localhost\ubuntu\home\x
    static string? DeUriVsCode(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) return null;
        if (u.Scheme == "file") return u.IsUnc ? $@"\\{u.Host}{Uri.UnescapeDataString(u.AbsolutePath).Replace('/', '\\')}" : u.LocalPath;
        var host = Uri.UnescapeDataString(u.Authority);
        if (u.Scheme == "vscode-remote" && host.StartsWith("wsl+", StringComparison.OrdinalIgnoreCase))
            return $@"\\wsl.localhost\{host[4..]}{Uri.UnescapeDataString(u.AbsolutePath).Replace('/', '\\')}";
        return null; // outros remotos (SSH, containers) ficam de fora
    }

    // Subpastas com .git (um nível abaixo da pasta listada, e a própria pasta se for um repositório).
    static IEnumerable<Projeto> Pasta(string raiz)
    {
        if (!Directory.Exists(raiz)) yield break;
        IEnumerable<string> candidatas;
        try { candidatas = Directory.EnumerateDirectories(raiz).Prepend(raiz).ToList(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { yield break; }

        foreach (var dir in candidatas)
        {
            var git = Path.Combine(dir, ".git");
            if (!Directory.Exists(git)) continue;
            // Atividade: último commit/checkout (índice do git) ou mudança na própria pasta.
            var indice = Path.Combine(git, "index");
            var quando = File.Exists(indice) ? File.GetLastWriteTime(indice) : Directory.GetLastWriteTime(dir);
            yield return new Projeto(Path.GetFileName(dir), dir, "pasta", quando, null);
        }
    }

    // Branch atual lendo .git/HEAD ("ref: refs/heads/main"); commit solto vira os 7 primeiros caracteres.
    static string? Branch(string pasta)
    {
        try
        {
            var head = Path.Combine(pasta, ".git", "HEAD");
            if (!File.Exists(head)) return null;
            var texto = File.ReadAllText(head).Trim();
            return texto.StartsWith("ref: refs/heads/") ? texto["ref: refs/heads/".Length..] : texto[..Math.Min(7, texto.Length)];
        }
        catch (IOException)
        {
            return null;
        }
    }

    // ---------- Abrir ----------

    public static string? Executavel(string editor, Editores cfg)
    {
        string? Configurado(string v) => string.IsNullOrWhiteSpace(v) ? null : Environment.ExpandEnvironmentVariables(v);
        var programas = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return editor switch
        {
            "vscode" => Configurado(cfg.VsCode) ?? Existente(Path.Combine(Local, "Programs", "Microsoft VS Code", "Code.exe"),
                                                              Path.Combine(programas, "Microsoft VS Code", "Code.exe")),
            "intellij" => Configurado(cfg.Intellij) ?? MaisNovo(Path.Combine(programas, "JetBrains"), "IntelliJ IDEA*", @"bin\idea64.exe"),
            "androidStudio" => Configurado(cfg.AndroidStudio) ?? Existente(Path.Combine(programas, "Android", "Android Studio", "bin", "studio64.exe")),
            _ => null,
        };
    }

    static string? Existente(params string[] caminhos) => caminhos.FirstOrDefault(File.Exists);

    static string? MaisNovo(string raiz, string padrao, string relativo)
    {
        if (!Directory.Exists(raiz)) return null;
        return Directory.EnumerateDirectories(raiz, padrao)
            .Select(d => Path.Combine(d, relativo))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    public static readonly (string Id, string Nome)[] Destinos =
    [
        ("vscode", "VS Code"), ("intellij", "IntelliJ IDEA"), ("androidStudio", "Android Studio"),
        ("terminal", "Terminal"), ("explorer", "Explorador de Arquivos"),
    ];

    // Abre o projeto no destino escolhido. Devolve o erro, se houver.
    public static string? Abrir(Projeto projeto, string destino, Editores editores)
    {
        try
        {
            ProcessStartInfo info;
            switch (destino)
            {
                case "terminal":
                    info = new ProcessStartInfo("wt.exe") { ArgumentList = { "-d", projeto.Caminho } };
                    break;
                case "explorer":
                    info = new ProcessStartInfo("explorer.exe") { ArgumentList = { projeto.Caminho } };
                    break;
                default:
                    var exe = Executavel(destino, editores);
                    if (exe is null) return $"Não encontrei o {Destinos.First(d => d.Id == destino).Nome}. Informe o caminho em Configurações → Projetos.";
                    info = new ProcessStartInfo(exe);
                    if (destino == "vscode" && projeto.Wsl)
                    {
                        // \\wsl.localhost\Ubuntu\home\x → vscode-remote://wsl+Ubuntu/home/x (abre no modo remoto do WSL)
                        var partes = projeto.Caminho.TrimStart('\\').Split('\\', 3);
                        info.ArgumentList.Add("--folder-uri");
                        info.ArgumentList.Add($"vscode-remote://wsl+{partes[1]}/{(partes.Length > 2 ? partes[2].Replace('\\', '/') : "")}");
                    }
                    else
                    {
                        info.ArgumentList.Add(projeto.Caminho);
                    }
                    break;
            }
            info.UseShellExecute = false;
            Process.Start(info);
            return null;
        }
        catch (Exception ex)
        {
            return $"Não foi possível abrir \"{projeto.Nome}\": {ex.Message}";
        }
    }
}
