using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Baterias: uma linha por aparelho Bluetooth (teclado, mouse, fones…) com a bolinha da faixa (verde/laranja/vermelho),
// o percentual e o ícone de bateria. Os dados vêm do ServicoBaterias; o botão do cabeçalho abre o Bluetooth do Windows.
public static class PainelBaterias
{
    static readonly Color Verde = Color.FromArgb(255, 61, 190, 139);
    static readonly Color Laranja = Color.FromArgb(255, 245, 158, 11);
    static readonly Color Vermelho = Color.FromArgb(255, 229, 72, 77);

    public static UIElement Criar(Baterias cfg, Estilo estilo, Button bluetooth)
    {
        bluetooth.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true }); }
            catch (Exception) { }
            _ = ServicoBaterias.Atualizar();
        };

        var lista = new StackPanel { Spacing = 2 };

        void Atualizar()
        {
            lista.Children.Clear();
            var aparelhos = ServicoBaterias.Atuais
                .Where(b => cfg.Visivel(b.Nome) && (b.Conectado || cfg.MostrarDesconectados)).ToList();
            if (aparelhos.Count == 0)
            {
                lista.Children.Add(new TextBlock
                {
                    Text = "Nenhum aparelho Bluetooth com bateria. Os que informam a bateria ao Windows aparecem aqui sozinhos.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.7,
                    FontFamily = estilo.Fonte,
                });
                return;
            }
            foreach (var b in aparelhos) lista.Children.Add(Linha(b, cfg, estilo));
        }

        lista.Loaded += (_, _) =>
        {
            ServicoBaterias.Mudou += Atualizar;
            Atualizar();
            _ = ServicoBaterias.Atualizar(); // ao abrir a Home, um valor fresco
        };
        lista.Unloaded += (_, _) => ServicoBaterias.Mudou -= Atualizar;
        return lista;
    }

    static Grid Linha(Bateria b, Baterias cfg, Estilo estilo)
    {
        var cor = b.Conectado ? Cor(b.Nivel, cfg) : Microsoft.UI.Colors.Gray;
        var icone = new FontIcon { Glyph = GlifoDoAparelho(b.Categoria), FontSize = estilo.Tam(20), VerticalAlignment = VerticalAlignment.Center };

        var bolinha = new Microsoft.UI.Xaml.Shapes.Ellipse
        {
            Width = 9,
            Height = 9,
            Fill = new SolidColorBrush(cor),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var nome = new TextBlock
        {
            Text = cfg.NomeDe(b.Nome),
            FontFamily = estilo.Fonte,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var detalhe = new TextBlock
        {
            Text = b.Conectado ? "Conectado"
                : b.Leitura is { } l ? $"Desconectado · última leitura {l.LocalDateTime:dd/MM HH:mm}" : "Desconectado",
            FontFamily = estilo.Fonte,
            FontSize = estilo.Legenda,
            Opacity = 0.7,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var cabeca = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { bolinha, nome } };
        var textos = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Children = { cabeca, detalhe } };

        var pinta = new SolidColorBrush(cor);
        var nivel = new TextBlock
        {
            Text = $"{b.Nivel}%",
            FontFamily = estilo.Fonte,
            FontSize = estilo.Realce,
            FontWeight = FontWeights.SemiBold,
            Foreground = pinta,
            MinWidth = 44,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var pilha = new FontIcon { Glyph = GlifoDaBateria(b.Nivel), FontSize = estilo.Titulo, Foreground = pinta, VerticalAlignment = VerticalAlignment.Center };
        var direita = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center, Children = { nivel, pilha } };

        var grade = new Grid { ColumnSpacing = 12, Padding = new Thickness(8, 6, 8, 6), Opacity = b.Conectado ? 1 : 0.55 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textos, 1);
        Grid.SetColumn(direita, 2);
        grade.Children.Add(icone);
        grade.Children.Add(textos);
        grade.Children.Add(direita);
        if (cfg.NomeDe(b.Nome) != b.Nome) ToolTipService.SetToolTip(grade, $"No Windows: {b.Nome}");
        return grade;
    }

    public static Color Cor(int nivel, Baterias cfg) => ServicoBaterias.FaixaDe(nivel, cfg) switch
    {
        ServicoBaterias.Faixa.Baixa => Vermelho,
        ServicoBaterias.Faixa.Media => Laranja,
        _ => Verde,
    };

    // Ícones do Segoe Fluent: bateria de 0 a 10 tracinhos (E850–E859; cheia = E83F).
    public static string GlifoDaBateria(int nivel)
    {
        var tracos = (int)Math.Round(Math.Clamp(nivel, 0, 100) / 10.0);
        return tracos >= 10 ? "" : ((char)(0xE850 + tracos)).ToString();
    }

    public static string GlifoDoAparelho(string categoria) => categoria switch
    {
        _ when categoria.StartsWith("Input.Keyboard") => "",
        _ when categoria.StartsWith("Input.Mouse") => "",
        _ when categoria.StartsWith("Input.Gaming") => "",
        _ when categoria.StartsWith("Input.Pen") => "",
        _ when categoria.StartsWith("Audio.Earbuds") || categoria.StartsWith("Audio.Headphone")
            || categoria.StartsWith("Communication.Headset") => "",
        _ when categoria.StartsWith("Audio.Speaker") => "",
        _ when categoria.StartsWith("Communication.Phone") => "",
        _ => "", // Bluetooth
    };
}
