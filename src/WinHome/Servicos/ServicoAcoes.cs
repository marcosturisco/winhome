using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Passo já resolvido: o comando exato (mostrado na confirmação) e como executá-lo.
public sealed record PassoPronto(string Nome, string Comando, bool Esperar, AppItem Item);

// Resultado de um passo: Ok = abriu/terminou bem; Texto = erro ou saída do comando; Esperou = tem saída para mostrar.
public sealed record ResultadoPasso(string Nome, bool Ok, string? Texto, bool Esperou);

// Ações rápidas: prepara os passos (troca {pastaProjetos}, encontra os editores) e os executa em ordem.
// Quem chama é responsável por pedir a confirmação do usuário antes de Executar.
public static class ServicoAcoes
{
    public const string MarcaPasta = "{pastaProjetos}";
    static readonly TimeSpan Limite = TimeSpan.FromSeconds(15);

    // Como o passo aparece nas Configurações (antes de resolver pastas e editores).
    public static string Descrever(PassoAcao p) =>
        p.App is { Length: > 0 } app ? $"app {app}"
        : p.Editor is { Length: > 0 } editor ? $"editor {editor} (detectado como em Projetos)"
        : $"{p.Caminho} {p.Args}".Trim() + (p.Esperar ? "  · espera e mostra o resultado" : "");

    public static (List<PassoPronto> Passos, List<string> Problemas) Preparar(AcaoRapida acao, Projetos projetos)
    {
        var passos = new List<PassoPronto>();
        var problemas = new List<string>();
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

            var usaPasta = (p.Caminho + p.Args).Contains(MarcaPasta, StringComparison.OrdinalIgnoreCase);
            if (usaPasta && projetos.Pastas.Count == 0)
            {
                problemas.Add($"{nome}: nenhuma pasta de projetos (use o botão Pastas… do painel Projetos).");
                continue;
            }
            foreach (var pasta in usaPasta ? projetos.Pastas : new List<string> { "" })
            {
                var caminho = Trocar(p.Caminho, pasta);
                var args = Trocar(p.Args ?? "", pasta);
                var rotulo = usaPasta && projetos.Pastas.Count > 1 ? $"{nome} ({Path.GetFileName(pasta.TrimEnd('\\'))})" : nome;
                passos.Add(new(rotulo, $"{caminho} {args}".Trim(), p.Esperar, new AppItem { Nome = rotulo, Caminho = caminho, Args = args }));
            }
        }
        return (passos, problemas);
    }

    // Sem barra no fim: em "-d \"D:\\\"" a barra escaparia a aspa.
    static string Trocar(string texto, string pasta)
    {
        var p = pasta.TrimEnd('\\');
        if (p.EndsWith(':')) p += @"\.";
        return texto.Replace(MarcaPasta, p, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<List<ResultadoPasso>> Executar(IEnumerable<PassoPronto> passos)
    {
        var resultados = new List<ResultadoPasso>();
        foreach (var p in passos)
        {
            if (p.Esperar)
            {
                var (ok, texto) = await Rodar(p.Item.Caminho!, p.Item.Args ?? "");
                resultados.Add(new(p.Nome, ok, texto, true));
            }
            else
            {
                var erro = Lancador.Abrir(p.Item);
                resultados.Add(new(p.Nome, erro is null, erro, false));
            }
        }
        return resultados;
    }

    // Roda escondido e junta a saída. Não espera o fim dos fluxos: o adb, por exemplo, pode deixar o
    // servidor dele rodando com a saída herdada.
    static async Task<(bool Ok, string Texto)> Rodar(string caminho, string args)
    {
        var saida = new StringBuilder();
        using var processo = new Process
        {
            StartInfo = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(caminho), args)
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
        if (!terminou) return (false, $"ainda rodando após {Limite.TotalSeconds:0} s" + (texto.Length > 0 ? $": {texto}" : ""));
        return (processo.ExitCode == 0, texto.Length > 0 ? texto : $"terminou (código {processo.ExitCode})");
    }
}
