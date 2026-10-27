using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Passo já resolvido: o comando exato (mostrado na confirmação) e como executá-lo.
public sealed record PassoPronto(string Nome, string Comando, bool Esperar, AppItem Item);

// Resultado de um passo: Ok = abriu/terminou bem; Texto = erro ou saída do comando; Esperou = tem saída para mostrar.
public sealed record ResultadoPasso(string Nome, bool Ok, string? Texto, bool Esperou);

// Ações rápidas: prepara os passos (troca os marcadores, encontra editores e programas) e os executa em ordem.
// Quem chama é responsável por pedir a confirmação do usuário antes de Executar.
public static partial class ServicoAcoes
{
    public const string MarcaPasta = "{pastaProjetos}";
    public const string MarcaArquivo = "{arquivo}";
    const string MarcaImagens = "{imagens}";
    const string MarcaDataHora = "{dataHora}";
    static readonly TimeSpan Limite = TimeSpan.FromMinutes(2); // instalar um APK grande demora

    public static readonly string PastaImagens =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "WinHome");

    // Como o passo aparece nas Configurações (antes de resolver marcadores e editores).
    public static string Descrever(PassoAcao p) =>
        p.App is { Length: > 0 } app ? $"app {app}"
        : p.Editor is { Length: > 0 } editor ? $"editor {editor} (detectado como em Projetos)"
        : $"{p.Caminho} {p.Args}".Trim() + (p.Esperar ? "  · espera e mostra o resultado" : "");

    public static bool PedeArquivo(AcaoRapida acao) =>
        acao.Passos.Any(p => (p.Caminho + p.Args).Contains(MarcaArquivo, StringComparison.OrdinalIgnoreCase));

    // arquivo = escolhido pelo usuário quando algum passo usa {arquivo}.
    public static (List<PassoPronto> Passos, List<string> Problemas) Preparar(AcaoRapida acao, Projetos projetos, string? arquivo = null)
    {
        var passos = new List<PassoPronto>();
        var problemas = new List<string>();
        var dataHora = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"); // o mesmo em todos os passos da ação
        foreach (var p in acao.Passos)
        {
            var nome = string.IsNullOrWhiteSpace(p.Nome) ? p.App ?? p.Editor ?? p.Caminho ?? "?" : p.Nome;

            if (p.App is { Length: > 0 } app)
            {
                passos.Add(new(nome, $@"explorer.exe shell:AppsFolder\{app}", false, new AppItem { Nome = nome, App = app }));
                continue;
            }
            if (p.Editor is { Length: > 0 } editor)
            {
                if (ServicoProjetos.Executavel(editor, projetos.Editores) is { } exe)
                    passos.Add(new(nome, $"\"{exe}\"", false, new AppItem { Nome = nome, Caminho = exe }));
                else
                    problemas.Add($"{nome}: editor \"{editor}\" não encontrado (informe o caminho em Configurações → Projetos).");
                continue;
            }
            if (string.IsNullOrWhiteSpace(p.Caminho))
            {
                problemas.Add($"{nome}: o passo não tem \"app\", \"editor\" nem \"caminho\".");
                continue;
            }

            var texto = p.Caminho + p.Args;
            if (texto.Contains(MarcaArquivo, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(arquivo))
            {
                problemas.Add($"{nome}: nenhum arquivo escolhido.");
                continue;
            }
            var usaPasta = texto.Contains(MarcaPasta, StringComparison.OrdinalIgnoreCase);
            if (usaPasta && projetos.Pastas.Count == 0)
            {
                problemas.Add($"{nome}: nenhuma pasta de projetos (use o botão Pastas… do painel Projetos).");
                continue;
            }
            foreach (var pasta in usaPasta ? projetos.Pastas : new List<string> { "" })
            {
                string Trocar(string t) => t
                    .Replace(MarcaPasta, SemBarraFinal(pasta), StringComparison.OrdinalIgnoreCase)
                    .Replace(MarcaImagens, PastaImagens, StringComparison.OrdinalIgnoreCase)
                    .Replace(MarcaDataHora, dataHora, StringComparison.OrdinalIgnoreCase)
                    .Replace(MarcaArquivo, arquivo ?? "", StringComparison.OrdinalIgnoreCase);
                var caminho = Localizar(Trocar(p.Caminho));
                var args = Trocar(p.Args ?? "");
                var rotulo = usaPasta && projetos.Pastas.Count > 1 ? $"{nome} ({Path.GetFileName(pasta.TrimEnd('\\'))})" : nome;
                var exibido = caminho.Contains(' ') ? $"\"{caminho}\"" : caminho;
                passos.Add(new(rotulo, $"{exibido} {args}".Trim(), p.Esperar, new AppItem { Nome = rotulo, Caminho = caminho, Args = args }));
            }
        }
        return (passos, problemas);
    }

    // Sem barra no fim: em "-d \"D:\\\"" a barra escaparia a aspa.
    static string SemBarraFinal(string pasta)
    {
        var p = pasta.TrimEnd('\\');
        return p.EndsWith(':') ? p + @"\." : p;
    }

    // Programa sem pasta ("adb", "scrcpy"): procura no PATH atual do usuário (lido do registro, para achar o que foi
    // instalado depois que a Home abriu). Não achando, devolve como veio e o Windows tenta do jeito dele.
    static string Localizar(string caminho)
    {
        caminho = Environment.ExpandEnvironmentVariables(caminho);
        if (caminho.Contains('\\') || caminho.Contains('/') || caminho.Contains(':')) return caminho;
        var nomes = Path.HasExtension(caminho) ? [caminho] : new[] { caminho + ".exe", caminho + ".cmd", caminho + ".bat" };
        var pastas = new[]
            {
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
            }
            .SelectMany(p => (p ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        foreach (var pasta in pastas)
            foreach (var nome in nomes)
            {
                var completo = Path.Combine(Environment.ExpandEnvironmentVariables(pasta), nome);
                if (File.Exists(completo)) return completo;
            }
        return caminho;
    }

    public static async Task<List<ResultadoPasso>> Executar(IEnumerable<PassoPronto> passos)
    {
        Directory.CreateDirectory(PastaImagens); // destino do {imagens}; vazia não incomoda
        var resultados = new List<ResultadoPasso>();
        string? falhou = null;
        foreach (var p in passos)
        {
            if (falhou is not null)
            {
                resultados.Add(new(p.Nome, false, $"pulado porque \"{falhou}\" falhou", false));
                continue;
            }
            if (p.Esperar)
            {
                var (ok, texto) = await Rodar(p.Item.Caminho!, p.Item.Args ?? "");
                resultados.Add(new(p.Nome, ok, texto, true));
                if (!ok) falhou = p.Nome; // ex.: TV desligada; não adianta seguir
            }
            else
            {
                var erro = Abrir(p.Item);
                resultados.Add(new(p.Nome, erro is null, erro, false));
            }
        }
        return resultados;
    }

    // Programas (.exe) abrem com a variável ADB apontando para o adb do PATH: o scrcpy traz um adb próprio e,
    // se usasse o dele, reiniciaria o servidor do adb e derrubaria a conexão sem fio. O resto abre como os apps.
    static string? Abrir(AppItem item)
    {
        if (item.Caminho is not { } caminho || !caminho.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(caminho))
            return Lancador.Abrir(item);
        try
        {
            var info = new ProcessStartInfo(caminho, item.Args ?? "")
            {
                UseShellExecute = false,
                CreateNoWindow = true, // programas de console (scrcpy) sem a janela preta; janelas normais não mudam
                WorkingDirectory = Path.GetDirectoryName(caminho) ?? "",
            };
            if (Localizar("adb") is { } adb && File.Exists(adb)) info.Environment["ADB"] = adb;
            Process.Start(info);
            return null;
        }
        catch (Win32Exception ex)
        {
            return $"Não foi possível abrir \"{item.Nome}\": {ex.Message}";
        }
    }

    // O adb sai com código 0 mesmo quando não conecta; o texto da saída denuncia a falha.
    [GeneratedRegex(@"\b(cannot|failed|unable|failure|no devices|not found|offline)\b|\berror:", RegexOptions.IgnoreCase)]
    private static partial Regex TextoDeFalha();

    // Roda escondido e junta a saída. Não espera o fim dos fluxos: o adb, por exemplo, pode deixar o
    // servidor dele rodando com a saída herdada.
    static async Task<(bool Ok, string Texto)> Rodar(string caminho, string args)
    {
        var saida = new StringBuilder();
        using var processo = new Process
        {
            StartInfo = new ProcessStartInfo(caminho, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };
        void Linha(object _, DataReceivedEventArgs e)
        {
            if (e.Data is { Length: > 0 } l) lock (saida) saida.AppendLine(l);
        }
        processo.OutputDataReceived += Linha;
        processo.ErrorDataReceived += Linha;
        try
        {
            processo.Start();
        }
        catch (Win32Exception ex)
        {
            return (false, $"Não foi possível executar \"{caminho}\": {ex.Message}");
        }
        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();

        var terminou = await Task.Run(() => processo.WaitForExit(Limite));
        await Task.Delay(150); // últimas linhas ainda a caminho
        string texto;
        lock (saida) texto = saida.ToString().Trim();
        if (!terminou) return (false, $"ainda rodando após {Limite.TotalMinutes:0} min" + (texto.Length > 0 ? $": {texto}" : ""));
        var ok = processo.ExitCode == 0 && !TextoDeFalha().IsMatch(texto);
        return (ok, texto.Length > 0 ? texto : $"terminou (código {processo.ExitCode})");
    }
}
