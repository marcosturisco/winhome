namespace WinHome.Configuracao;

// Tema com nome: os prontos (abaixo) e os que o usuário salva ("temasSalvos" na config).
public sealed class TemaSalvo
{
    public string Nome { get; set; } = "";
    public Tema Tema { get; set; } = new();
}

// Temas prontos da tela de Aparência. Aplicar um tema muda só o visual (modo, cores, fundo, fonte, cantos,
// opacidade); colunas, espaçamento e tamanho dos ícones ficam como estão, para a disposição da Home não mudar.
public static class TemasProntos
{
    public static readonly IReadOnlyList<TemaSalvo> Lista =
    [
        new() { Nome = "Padrão", Tema = new() { Modo = "escuro", CorDestaque = "#4F8CFF", Fundo = "mica", Fonte = "Segoe UI Variable", Cantos = 12, OpacidadePaineis = 0.6 } },
        new() { Nome = "Grafite", Tema = new() { Modo = "escuro", CorDestaque = "#C8CDD2", Fundo = "cor", CorFundo = "#0E0F11", Fonte = "Segoe UI Variable", Cantos = 2, OpacidadePaineis = 0.85 } },
        new() { Nome = "Oceano", Tema = new() { Modo = "escuro", CorDestaque = "#2EC4D6", Fundo = "acrilico", Fonte = "Segoe UI Variable", Cantos = 16, OpacidadePaineis = 0.5 } },
        new() { Nome = "Claro", Tema = new() { Modo = "claro", CorDestaque = "#2563EB", Fundo = "mica", Fonte = "Segoe UI Variable", Cantos = 12, OpacidadePaineis = 0.7 } },
        new() { Nome = "Sépia", Tema = new() { Modo = "claro", CorDestaque = "#C2581C", Fundo = "cor", CorFundo = "#F3E9D8", Fonte = "Georgia", Cantos = 8, OpacidadePaineis = 0.75 } },
        new() { Nome = "Alto contraste", Tema = new() { Modo = "escuro", CorDestaque = "#FFD400", Fundo = "cor", CorFundo = "#000000", Fonte = "Segoe UI Variable", Cantos = 0, OpacidadePaineis = 1 } },
    ];

    // Copia só o visual do modelo para o tema da config.
    public static void Aplicar(Tema modelo, Tema destino)
    {
        destino.Modo = modelo.Modo;
        destino.CorDestaque = modelo.CorDestaque;
        destino.Fundo = modelo.Fundo;
        destino.CorFundo = modelo.CorFundo;
        if (modelo.Fundo == "imagem") destino.ImagemFundo = modelo.ImagemFundo;
        destino.Fonte = modelo.Fonte;
        destino.Cantos = modelo.Cantos;
        destino.OpacidadePaineis = modelo.OpacidadePaineis;
    }

    public static Tema Copiar(Tema t) => new()
    {
        Modo = t.Modo, CorDestaque = t.CorDestaque, Fundo = t.Fundo, CorFundo = t.CorFundo, ImagemFundo = t.ImagemFundo,
        Fonte = t.Fonte, TamanhoTexto = t.TamanhoTexto, Cantos = t.Cantos, TamanhoIcone = t.TamanhoIcone, OpacidadePaineis = t.OpacidadePaineis,
        Espacamento = t.Espacamento, Colunas = t.Colunas,
    };

    // O visual do tema atual é igual ao deste? (para marcar o tema escolhido na galeria)
    public static bool Igual(Tema a, Tema b) =>
        a.Modo == b.Modo && string.Equals(a.CorDestaque, b.CorDestaque, StringComparison.OrdinalIgnoreCase) &&
        a.Fundo == b.Fundo && (a.Fundo != "cor" || string.Equals(a.CorFundo, b.CorFundo, StringComparison.OrdinalIgnoreCase)) &&
        a.Fonte == b.Fonte && Math.Abs(a.Cantos - b.Cantos) < 0.5 && Math.Abs(a.OpacidadePaineis - b.OpacidadePaineis) < 0.01;
}
