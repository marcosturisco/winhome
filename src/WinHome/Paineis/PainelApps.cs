using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Grade de botões de um grupo de apps; navegável por setas, Enter abre.
public static class PainelApps
{
    public static UIElement Criar(List<AppItem> apps, Estilo estilo, Action<AppItem> abrir)
    {
        var icone = estilo.TamanhoIcone;
        var grade = new VariableSizedWrapGrid
        {
            Orientation = Orientation.Horizontal,
            ItemWidth = icone + 56,
            ItemHeight = icone + 52,
            XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled,
        };

        foreach (var app in apps)
            grade.Children.Add(Botao(app, estilo, abrir));

        if (apps.Count == 0)
            return new TextBlock { Text = "Nenhum app neste grupo.", Opacity = 0.7, FontFamily = estilo.Fonte };
        return grade;
    }

    static Button Botao(AppItem app, Estilo estilo, Action<AppItem> abrir)
    {
        var tamanho = estilo.TamanhoIcone;
        UIElement figura = Icones.De(app, tamanho) is { } imagem
            ? new Image { Source = imagem, Width = tamanho, Height = tamanho }
            : Inicial(app, estilo);

        var botao = new Button
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(2),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.6)),
            Content = new StackPanel
            {
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    figura,
                    new TextBlock
                    {
                        Text = app.Nome,
                        FontFamily = estilo.Fonte,
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = tamanho + 44,
                    },
                },
            },
        };
        ToolTipService.SetToolTip(botao, app.Nome);
        botao.Click += (_, _) => abrir(app);
        return botao;
    }

    // Sem ícone: primeira letra do nome num quadrado com a cor de destaque.
    static UIElement Inicial(AppItem app, Estilo estilo) => new Border
    {
        Width = estilo.TamanhoIcone,
        Height = estilo.TamanhoIcone,
        CornerRadius = new CornerRadius(estilo.TamanhoIcone / 4.0),
        Background = estilo.Destaque,
        Child = new TextBlock
        {
            Text = app.Nome.Length > 0 ? app.Nome[..1].ToUpperInvariant() : "?",
            FontFamily = estilo.Fonte,
            FontSize = estilo.TamanhoIcone / 2.0,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
