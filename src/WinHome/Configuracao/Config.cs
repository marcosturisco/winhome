namespace WinHome.Configuracao;

// Modelo do config.json. Toda propriedade tem valor padrão: opção ausente no arquivo = padrão.
public sealed class Config
{
    public string Nome { get; set; } = "";
    public string Janela { get; set; } = "maximizada";
    public string Atalho { get; set; } = "Ctrl+Alt+Space";
    public bool IniciarComWindows { get; set; }
    public bool EsconderAoAbrirApp { get; set; } = true;
    public Tema Tema { get; set; } = new();
    public List<Painel> Paineis { get; set; } = [];
    public Dictionary<string, List<AppItem>> Grupos { get; set; } = [];
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
    public double OpacidadePaineis { get; set; } = 0.35;
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
