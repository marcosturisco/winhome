using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Ações rápidas: um botão por ação; o clique mostra os comandos exatos e só executa depois da confirmação.
public static class PainelAcoes
{
    static bool _confirmando; // uma confirmação por vez (o ContentDialog não aceita duas abertas)

    public static UIElement Criar(Config config, Estilo estilo, Acoes acoes)
    {
        if (config.AcoesRapidas.Count == 0)
            return new TextBlock
            {
                Text = "Nenhuma ação. Crie as suas em Configurações → Ações rápidas.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontFamily = estilo.Fonte,
            };

        var grade = new VariableSizedWrapGrid
        {
            Orientation = Orientation.Horizontal,
            ItemWidth = 132,
            ItemHeight = 104,
            XYFocusKeyboardNavigation = Microsoft.UI.Xaml.Input.XYFocusKeyboardNavigationMode.Enabled,
        };
        foreach (var acao in config.AcoesRapidas) grade.Children.Add(Botao(acao, config.Projetos, estilo, acoes));
        return grade;
    }

    static Button Botao(AcaoRapida acao, Projetos projetos, Estilo estilo, Acoes acoes)
    {
        var conteudo = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(acao.Icone) ? "⚡" : acao.Icone,
                    FontFamily = new FontFamily("Segoe UI Emoji"),
                    FontSize = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
                new TextBlock
                {
                    Text = acao.Nome,
                    FontFamily = estilo.Fonte,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxLines = 2,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                },
            },
        };
        var botao = new Button
        {
            Content = conteudo,
            Margin = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.5)),
        };
        ToolTipService.SetToolTip(botao, acao.Passos.Count == 0
            ? "Ação sem passos"
            : string.Join("\n", acao.Passos.Select(p => "• " + (string.IsNullOrWhiteSpace(p.Nome) ? ServicoAcoes.Descrever(p) : p.Nome))));

        botao.Click += async (_, _) =>
        {
            if (_confirmando) return;
            var (passos, problemas) = ServicoAcoes.Preparar(acao, projetos);
            if (passos.Count == 0)
            {
                acoes.Resultado(problemas.Count > 0 ? string.Join("\n", problemas) : $"A ação \"{acao.Nome}\" não tem passos.");
                return;
            }

            _confirmando = true;
            bool confirmou;
            try { confirmou = await Confirmar(botao.XamlRoot, acao, passos, problemas); }
            finally { _confirmando = false; }
            if (!confirmou) return;

            botao.IsEnabled = false;
            var resultados = await ServicoAcoes.Executar(passos);
            botao.IsEnabled = true;

            var erros = problemas.Concat(resultados.Where(r => !r.Ok).Select(r => $"{r.Nome}: {r.Texto}")).ToList();
            var saidas = resultados.Where(r => r.Ok && r.Esperou)
                .Select(r => $"{r.Nome}: {r.Texto}").ToList();
            if (erros.Count > 0) acoes.Resultado(string.Join("\n", erros.Concat(saidas)));
            else if (saidas.Count > 0) acoes.Informar(string.Join("\n", saidas)); // a Home fica aberta para mostrar
            else acoes.Resultado(null); // tudo aberto: esconde a Home, como ao abrir um app
        };
        return botao;
    }

    // Mostra cada comando exatamente como será executado; Enter executa, Esc cancela.
    static async Task<bool> Confirmar(XamlRoot raiz, AcaoRapida acao, List<PassoPronto> passos, List<string> problemas)
    {
        var lista = new StackPanel { Spacing = 10 };
        lista.Children.Add(new TextBlock { Text = "Estes comandos serão executados, nesta ordem:", TextWrapping = TextWrapping.Wrap });
        foreach (var p in passos)
        {
            lista.Children.Add(new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = p.Esperar ? $"{p.Nome}  ·  espera e mostra o resultado" : p.Nome,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = p.Comando,
                        FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                        FontSize = 12,
                        Opacity = 0.8,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true,
                    },
                },
            });
        }
        foreach (var problema in problemas)
            lista.Children.Add(new TextBlock
            {
                Text = $"Será pulado — {problema}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
            });

        var dialogo = new ContentDialog
        {
            XamlRoot = raiz,
            Title = $"{acao.Icone} {acao.Nome}",
            Content = new ScrollViewer { Content = lista, MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            PrimaryButtonText = "Executar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            RequestedTheme = (raiz.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default,
        };
        return await dialogo.ShowAsync() == ContentDialogResult.Primary;
    }
}
