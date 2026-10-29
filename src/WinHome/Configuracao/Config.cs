namespace WinHome.Configuracao;

// Modelo do config.json. Toda propriedade tem valor padrão: opção ausente no arquivo = padrão.
public sealed class Config
{
    public string Nome { get; set; } = "";
    public string Janela { get; set; } = "maximizada";
    public string Atalho { get; set; } = "Ctrl+Alt+Space";
    public bool IniciarComWindows { get; set; }
    public bool EsconderAoAbrirApp { get; set; } = true;
    public bool MostrarBusca { get; set; } = true;
    public bool IconeNaBandeja { get; set; } = true;
    public bool ModoAreaDeTrabalho { get; set; } // Home atrás das janelas, no lugar do papel de parede
    public Tema Tema { get; set; } = new();
    public List<TemaSalvo> TemasSalvos { get; set; } = []; // "Salvar o visual atual como tema"
    public BoasVindas BoasVindas { get; set; } = new();
    public Mensagens Mensagens { get; set; } = new();
    public Curiosidades Curiosidades { get; set; } = new();
    public Projetos Projetos { get; set; } = new();
    public Tarefas Tarefas { get; set; } = new();
    public List<AcaoRapida> AcoesRapidas { get; set; } = AcaoRapida.Padrao();
    public Videos Videos { get; set; } = new();
    public Dictionary<string, AppAncorado> AppsAncorados { get; set; } = []; // id do painel → app
    public Dispositivos Dispositivos { get; set; } = new();
    public Baterias Baterias { get; set; } = new();
    public Agenda Agenda { get; set; } = new();
    public List<Painel> Paineis { get; set; } = [];
    public Dictionary<string, List<AppItem>> Grupos { get; set; } = [];

    // Ids dos painéis que não são grupos de apps.
    public const string PainelBoasVindas = "boasVindas";
    public const string PainelMensagem = "mensagem";
    public const string PainelCuriosidade = "curiosidade";
    public const string PainelProjetos = "projetos";
    public const string PainelTarefas = "tarefas";
    public const string PainelAcoes = "acoes";
    public const string PainelVideos = "videos";
    public const string PainelDispositivos = "dispositivos";
    public const string PainelAgenda = "agenda";
    public const string PainelBaterias = "baterias";

    // Ajustes de configs antigas, feitos ao carregar (a gravação seguinte já sai no formato novo).
    public void Migrar()
    {
        foreach (var p in Paineis.Where(p => p.Id == "relogio")) p.Id = PainelBoasVindas; // até a v0.2
        // Ações padrão gravadas antes de existir "descricao" (v0.4-c): ganham a descrição pelo nome.
        foreach (var padrao in AcaoRapida.Padrao())
            foreach (var a in AcoesRapidas.Where(a => a.Nome == padrao.Nome && string.IsNullOrWhiteSpace(a.Descricao)))
                a.Descricao = padrao.Descricao;
    }
}

// Painel do topo: saudação, hora e data à esquerda; clima e cotações à direita.
public sealed class BoasVindas
{
    public bool MostrarSaudacao { get; set; } = true;
    public bool MostrarData { get; set; } = true;
    public bool MostrarSegundos { get; set; }
    public Clima Clima { get; set; } = new();
    public Cotacoes Cotacoes { get; set; } = new();
}

public sealed class Clima
{
    public bool Visivel { get; set; } = true;
    public string Unidade { get; set; } = "C"; // "C" ou "F"
    public List<Cidade> Cidades { get; set; } =
    [
        new() { Nome = "São Paulo", Regiao = "São Paulo", Pais = "Brasil", Latitude = -23.5475, Longitude = -46.63611, Fuso = "America/Sao_Paulo" },
        new() { Nome = "Orlando", Regiao = "Flórida", Pais = "EUA", Latitude = 28.53834, Longitude = -81.37924, Fuso = "America/New_York" },
    ];
}

// Coordenadas e fuso vêm da busca de cidades (Open-Meteo) ao adicionar pela tela de Configurações.
public sealed class Cidade
{
    public string Nome { get; set; } = "";
    public string? Regiao { get; set; }
    public string? Pais { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Fuso { get; set; }
}

public sealed class Cotacoes
{
    public bool Visivel { get; set; } = true;
    public List<string> Moedas { get; set; } = ["USD", "EUR"]; // sempre em relação ao real (BRL)
}

public sealed class Mensagens
{
    public string Fonte { get; set; } = "ambas"; // "ambas", "embutidas" ou "minhas"
    public List<Frase> Minhas { get; set; } = [];
}

// Painel "Curiosidades do dia" (Wikipédia "Neste dia"): destacados e eventos sempre; nascimentos/mortes opcionais.
public sealed class Curiosidades
{
    public bool IncluirNascimentos { get; set; }
    public bool IncluirMortes { get; set; }
}

// Painel "Projetos recentes": histórico das IDEs + subpastas com git das pastas listadas.
public sealed class Projetos
{
    public bool UsarIntellij { get; set; } = true;
    public bool UsarAndroidStudio { get; set; } = true;
    public bool UsarVsCode { get; set; } = true;
    public List<string> Pastas { get; set; } = [@"D:\Documents\dev"];
    public int Maximo { get; set; } = 8;
    public string EditorPadrao { get; set; } = "origem"; // "origem" (a IDE onde foi aberto), "vscode", "intellij", "androidStudio"
    public List<string> Fixados { get; set; } = [];
    public List<string> Ocultos { get; set; } = [];
    public Editores Editores { get; set; } = new();
}

// Painel "Tarefas": a lista fica em tarefas.json (ServicoTarefas), fora da config.
public sealed class Tarefas
{
    public bool MostrarConcluidas { get; set; } = true;
}

// Painel "Agenda": compromissos dos próximos "dias" (1 = só hoje) lidos do "Endereço secreto no formato iCal"
// do Google Agenda. O endereço dá acesso de leitura à agenda: fica só neste config.json (nunca em git ou log).
public sealed class Agenda
{
    public int Dias { get; set; } = 2;
    public List<Calendario> Calendarios { get; set; } = [];
}

public sealed class Calendario
{
    public string Nome { get; set; } = "Minha agenda";
    public string Endereco { get; set; } = ""; // secreto
    public string Cor { get; set; } = "#4F8CFF";
}

// Painel "Baterias": aparelhos Bluetooth que informam a bateria ao Windows (teclado, mouse, fones…), lidos a cada
// minuto. Abaixo de "baixa" fica vermelho e gera um aviso; abaixo de "media", laranja; senão, verde.
public sealed class Baterias
{
    public int Baixa { get; set; } = 20;
    public int Media { get; set; } = 50;
    public bool Avisar { get; set; } = true;             // notificação do Windows ao ficar abaixo de "baixa"
    public bool AlertaNoBoasVindas { get; set; } = true; // selo no Boas-vindas enquanto algum estiver abaixo de "baixa"
    public bool MostrarDesconectados { get; set; } = true;
    public List<string> Ocultos { get; set; } = [];                // nomes como o Windows mostra
    public Dictionary<string, string> Apelidos { get; set; } = []; // nome no Windows → nome na Home

    public bool Visivel(string nome) => !Ocultos.Contains(nome);

    public string NomeDe(string nome) => Apelidos.TryGetValue(nome, out var a) && a.Trim() is { Length: > 0 } t ? t : nome;
}

// Painel "Dispositivos": testa a cada "intervalo" segundos se a porta de cada aparelho responde (só abre e fecha
// uma conexão TCP; nenhum comando é enviado). Clicar no aparelho mostra as ações do adb para ele.
public sealed class Dispositivos
{
    public int Intervalo { get; set; } = 60;
    public List<Dispositivo> Lista { get; set; } =
    [
        new() { Nome = "TV", Icone = "📺", Endereco = "192.168.15.11", Porta = 5555 },
        new() { Nome = "Box", Icone = "📡", Endereco = "192.168.15.13", Porta = 5555 },
    ];
}

public sealed class Dispositivo
{
    public string Nome { get; set; } = "";
    public string Icone { get; set; } = "📱";
    public string Endereco { get; set; } = "";
    public int Porta { get; set; } = 5555;

    [System.Text.Json.Serialization.JsonIgnore]
    public string Alvo => $"{Endereco}:{Porta}"; // como o adb identifica o aparelho

    // Ações do adb para este aparelho (menu do painel Dispositivos); executam com a mesma confirmação das Ações rápidas.
    public List<AcaoRapida> Acoes()
    {
        var conectar = new PassoAcao { Nome = $"Conectar a {Nome}", Caminho = "adb", Args = $"connect {Alvo}", Esperar = true };
        return
        [
            new()
            {
                Nome = $"Conectar {Nome}", Icone = "🔗",
                Descricao = $"Liga a depuração sem fio com {Nome}: o aparelho aparece no Android Studio e aceita os comandos do adb. Não muda nada nele.",
                Passos = [conectar],
            },
            new()
            {
                Nome = $"Espelhar {Nome}", Icone = "🪞",
                Descricao = $"Mostra a tela de {Nome} numa janela do PC e deixa controlar com mouse e teclado (scrcpy): " +
                            "clique = toque/OK, botão direito = voltar, Ctrl+H = Home.",
                Passos = [conectar, new() { Nome = "Espelhar com o scrcpy", Caminho = "scrcpy", Args = $"-s {Alvo} --window-title \"{Nome}\"" }],
            },
            new()
            {
                Nome = $"Print de {Nome}", Icone = "📸",
                Descricao = $"Tira um print da tela de {Nome}, salva em Imagens\\WinHome e abre a imagem.",
                Passos =
                [
                    conectar,
                    new() { Nome = "Capturar a tela", Caminho = "adb", Args = $"-s {Alvo} shell screencap -p /sdcard/winhome-print.png", Esperar = true },
                    new() { Nome = "Copiar o print para o PC", Caminho = "adb", Args = $"-s {Alvo} pull /sdcard/winhome-print.png \"{{imagens}}\\{Nome}-{{dataHora}}.png\"", Esperar = true },
                    new() { Nome = "Abrir o print", Caminho = $"{{imagens}}\\{Nome}-{{dataHora}}.png" },
                ],
            },
            new()
            {
                Nome = $"Logs de {Nome}", Icone = "📜",
                Descricao = $"Abre um Terminal com o log de {Nome} ao vivo (adb logcat). Ctrl+C ou fechar a aba encerra.",
                Passos = [conectar, new() { Nome = "Log ao vivo no Terminal", Caminho = "wt.exe", Args = $"--title \"Logs de {Nome}\" adb -s {Alvo} logcat -v color" }],
            },
            new()
            {
                Nome = $"Instalar APK em {Nome}", Icone = "📦", TipoArquivo = ".apk",
                Descricao = $"Escolha um arquivo .apk e ele é instalado em {Nome} (atualiza se o app já existir, mantendo os dados).",
                Passos = [conectar, new() { Nome = "Instalar o APK", Caminho = "adb", Args = $"-s {Alvo} install -r \"{{arquivo}}\"", Esperar = true }],
            },
            new()
            {
                Nome = $"Desconectar {Nome}", Icone = "🔌",
                Descricao = $"Encerra a depuração sem fio com {Nome}.",
                Passos = [new() { Nome = $"Desconectar {Nome}", Caminho = "adb", Args = $"disconnect {Alvo}", Esperar = true }],
            },
        ];
    }
}

// Painel "App ancorado": a janela real do app fica posicionada sobre a área do painel e acompanha a Home.
// Abre por "editor" (vscode, intellij, androidStudio), "app" (AppID) ou "caminho"; "processo" é o nome do processo
// dono da janela (ex.: idea64, Code, WindowsTerminal) — vazio = nome do executável.
public sealed class AppAncorado
{
    public string Nome { get; set; } = "";
    public string? Editor { get; set; }
    public string? App { get; set; }
    public string? Caminho { get; set; }
    public string? Args { get; set; }
    public string? Processo { get; set; }
    public int Altura { get; set; } = 640; // altura da área do app, em pixels da Home
}

// Painel "Vídeos": arquivos e links pelo VLC instalado; links do YouTube pelo player do YouTube (WebView2).
// Volume e recentes ficam em videos.json (EstadoVideos), fora da config.
public sealed class Videos
{
    public bool Repetir { get; set; }
    public bool PausarAoEsconder { get; set; } = true;
    public string PastaVlc { get; set; } = ""; // vazio = C:\Program Files\VideoLAN\VLC
}

// Painel "Ações rápidas": cada ação executa os passos em ordem, sempre depois da confirmação do usuário.
public sealed class AcaoRapida
{
    public string Nome { get; set; } = "";
    public string Icone { get; set; } = "⚡"; // emoji
    public string Descricao { get; set; } = ""; // o que a ação faz e para que serve (dica e confirmação)
    public string TipoArquivo { get; set; } = "*"; // filtro do seletor quando algum passo usa {arquivo} (ex.: ".apk")
    public List<PassoAcao> Passos { get; set; } = [];

    const string Tv = "192.168.15.11:5555";

    // Antes de qualquer comando na TV: conecta (se já estiver conectada, o adb só confirma).
    static PassoAcao ConectarTv() => new() { Nome = "Conectar à TV", Caminho = "adb", Args = $"connect {Tv}", Esperar = true };

    public static List<AcaoRapida> Padrao() =>
    [
        new()
        {
            Nome = "Dia de trabalho",
            Icone = "💼",
            Descricao = "Abre de uma vez os programas do trabalho: VS Code, IntelliJ, Teams, Outlook e Edge.",
            Passos =
            [
                new() { Nome = "VS Code", Editor = "vscode" },
                new() { Nome = "IntelliJ IDEA", Editor = "intellij" },
                new() { Nome = "Teams", App = "MSTeams_8wekyb3d8bbwe!MSTeams" },
                new() { Nome = "Outlook", App = "Microsoft.OutlookForWindows_8wekyb3d8bbwe!Microsoft.OutlookforWindows" },
                new() { Nome = "Edge", App = "MSEdge" },
            ],
        },
        new()
        {
            Nome = "Abrir projetos",
            Icone = "📁",
            Descricao = "Abre a pasta de projetos (botão Pastas… do painel Projetos) no Explorador e no Terminal.",
            Passos =
            [
                new() { Nome = "Pasta no Explorador", Caminho = "explorer.exe", Args = "\"{pastaProjetos}\"" },
                new() { Nome = "Pasta no Terminal", Caminho = "wt.exe", Args = "-d \"{pastaProjetos}\"" },
            ],
        },
        new()
        {
            Nome = "Conectar TV",
            Icone = "📺",
            Descricao = "Liga a depuração sem fio com a TV. Depois disso, a TV aparece no Android Studio (Run/Debug sem cabo) " +
                        "e aceita os comandos do adb. Não muda nada na TV.",
            Passos = [ConectarTv()],
        },
        new()
        {
            Nome = "Espelhar TV",
            Icone = "🪞",
            Descricao = "Mostra a tela da TV numa janela do PC e deixa controlar com mouse e teclado (scrcpy): " +
                        "clique = toque/OK, botão direito = voltar, Ctrl+H = Home. Fechar a janela encerra o espelhamento.",
            Passos =
            [
                ConectarTv(),
                new() { Nome = "Espelhar com o scrcpy", Caminho = "scrcpy", Args = $"-s {Tv} --window-title \"TV\"" },
            ],
        },
        new()
        {
            Nome = "Print da TV",
            Icone = "📸",
            Descricao = "Tira um print do que está na tela da TV, salva em Imagens\\WinHome e abre a imagem.",
            Passos =
            [
                ConectarTv(),
                new() { Nome = "Capturar a tela na TV", Caminho = "adb", Args = $"-s {Tv} shell screencap -p /sdcard/winhome-print.png", Esperar = true },
                new() { Nome = "Copiar o print para o PC", Caminho = "adb", Args = $"-s {Tv} pull /sdcard/winhome-print.png \"{{imagens}}\\tv-{{dataHora}}.png\"", Esperar = true },
                new() { Nome = "Abrir o print", Caminho = @"{imagens}\tv-{dataHora}.png" },
            ],
        },
        new()
        {
            Nome = "Logs da TV",
            Icone = "📜",
            Descricao = "Abre um Terminal com o log da TV ao vivo (adb logcat), para acompanhar erros do launcher. " +
                        "Ctrl+C ou fechar a aba encerra.",
            Passos =
            [
                ConectarTv(),
                new() { Nome = "Log ao vivo no Terminal", Caminho = "wt.exe", Args = $"--title \"Logs da TV\" adb -s {Tv} logcat -v color" },
            ],
        },
        new()
        {
            Nome = "Instalar APK na TV",
            Icone = "📦",
            Descricao = "Escolha um arquivo .apk e ele é instalado na TV (atualiza se o app já existir, mantendo os dados).",
            TipoArquivo = ".apk",
            Passos =
            [
                ConectarTv(),
                new() { Nome = "Instalar o APK", Caminho = "adb", Args = $"-s {Tv} install -r \"{{arquivo}}\"", Esperar = true },
            ],
        },
        new()
        {
            Nome = "Desconectar TV",
            Icone = "🔌",
            Descricao = "Encerra a depuração sem fio com a TV (o Android Studio deixa de vê-la).",
            Passos = [new() { Nome = "Desconectar a TV", Caminho = "adb", Args = $"disconnect {Tv}", Esperar = true }],
        },
    ];
}

// Um passo usa "app" (AppID), "editor" (vscode, intellij, androidStudio: detectado como no painel Projetos)
// ou "caminho" (programa, pasta ou documento) com "args". Marcadores: {pastaProjetos} (repete o passo para cada
// pasta de Projetos), {imagens} (Imagens\WinHome), {dataHora} e {arquivo} (pede para escolher um arquivo).
public sealed class PassoAcao
{
    public string Nome { get; set; } = "";
    public string? App { get; set; }
    public string? Editor { get; set; }
    public string? Caminho { get; set; }
    public string? Args { get; set; }
    public bool Esperar { get; set; } // roda escondido, espera o fim (até 2 min) e mostra a saída; se falhar, pula o resto
}

// Caminhos dos executáveis; vazio = detectar sozinho.
public sealed class Editores
{
    public string VsCode { get; set; } = "";
    public string Intellij { get; set; } = "";
    public string AndroidStudio { get; set; } = "";
}

public sealed class Frase
{
    public string Texto { get; set; } = "";
    public string Autor { get; set; } = "";
}

public sealed class Tema
{
    public string Modo { get; set; } = "escuro";
    public string CorDestaque { get; set; } = "#4F8CFF";
    public string Fundo { get; set; } = "mica";
    public string CorFundo { get; set; } = "#101418";
    public string ImagemFundo { get; set; } = "";
    public string Fonte { get; set; } = "Segoe UI Variable";
    public double TamanhoTexto { get; set; } = 14; // texto normal dos painéis; os demais tamanhos acompanham (Estilo)
    public double Cantos { get; set; } = 12;
    public double TamanhoIcone { get; set; } = 48;
    public double OpacidadePaineis { get; set; } = 0.6;
    public double Espacamento { get; set; } = 16;
    public int Colunas { get; set; } = 6;
}

public sealed class Painel
{
    public string Id { get; set; } = "";
    public bool Visivel { get; set; } = true;
    public int Largura { get; set; } = 2;
    // Altura da linha da Home em pixels (0 = automática, pelo conteúdo). A linha usa a maior entre os painéis dela;
    // conteúdo maior ganha rolagem dentro do painel.
    public int Altura { get; set; }
    public string? Titulo { get; set; }

    public const int AlturaMinima = 100, AlturaMaxima = 2000;
}

public sealed class AppItem
{
    public string Nome { get; set; } = "";
    public string? App { get; set; }
    public string? Caminho { get; set; }
    public string? Args { get; set; }
    public string? Pasta { get; set; }
    public string? Icone { get; set; }
}
