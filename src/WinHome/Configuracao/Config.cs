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
    public Tema Tema { get; set; } = new();
    public BoasVindas BoasVindas { get; set; } = new();
    public Mensagens Mensagens { get; set; } = new();
    public List<Painel> Paineis { get; set; } = [];
    public Dictionary<string, List<AppItem>> Grupos { get; set; } = [];

    // Ids dos painéis que não são grupos de apps.
    public const string PainelBoasVindas = "boasVindas";
    public const string PainelMensagem = "mensagem";

    // Ajustes de configs antigas, feitos ao carregar (a gravação seguinte já sai no formato novo).
    public void Migrar()
    {
        foreach (var p in Paineis.Where(p => p.Id == "relogio")) p.Id = PainelBoasVindas; // até a v0.2
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
    public string? Titulo { get; set; }
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
