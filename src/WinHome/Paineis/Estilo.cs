using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Valores visuais já resolvidos a partir do tema da config, usados por todos os painéis.
public sealed class Estilo
{
    public required Tema Tema { get; init; }
    public required bool Escuro { get; init; }

    public Brush Destaque => new SolidColorBrush(Cor(Tema.CorDestaque, Color.FromArgb(255, 79, 140, 255)));
    public FontFamily Fonte => new(string.IsNullOrWhiteSpace(Tema.Fonte) ? "Segoe UI Variable" : Tema.Fonte);
    public CornerRadius Cantos => new(Math.Max(0, Tema.Cantos));
    public double Espaco => Math.Max(0, Tema.Espacamento);
    public int Colunas => Math.Clamp(Tema.Colunas, 1, 24);
    public int TamanhoIcone => (int)Math.Clamp(Tema.TamanhoIcone, 16, 256);

    // Tipografia: todo texto (e ícone de conteúdo) dos painéis usa estes tamanhos, proporcionais ao "tamanhoTexto"
    // do tema (14 = padrão). Tamanhos especiais (hora, temperatura, emojis) usam Tam(valor no tamanho padrão).
    public double Escala => Math.Clamp(Tema.TamanhoTexto, 10, 24) / 14.0;
    public double Tam(double tamanhoNoPadrao) => Math.Round(tamanhoNoPadrao * Escala, 1);
    public double Legenda => Tam(12); // detalhes, rodapés, botões pequenos
    public double Corpo => Tam(14);   // texto normal
    public double Realce => Tam(16);  // texto principal de um painel de leitura, valores em destaque
    public double Titulo => Tam(18);  // título dos painéis
    public double Grande => Tam(20);  // saudação, data, mensagem do dia

    public Brush FundoPainel
    {
        get
        {
            var alfa = (byte)(Math.Clamp(Tema.OpacidadePaineis, 0, 1) * 255);
            return new SolidColorBrush(Escuro ? Color.FromArgb(alfa, 32, 32, 36) : Color.FromArgb(alfa, 255, 255, 255));
        }
    }

    // "#RRGGBB" ou "#AARRGGBB"; texto inválido usa o padrão.
    public static Color Cor(string? hex, Color padrao)
    {
        var t = (hex ?? "").Trim().TrimStart('#');
        if (t.Length == 6) t = "FF" + t;
        if (t.Length != 8 || !uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            return padrao;
        return ColorHelper.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
    }
}
