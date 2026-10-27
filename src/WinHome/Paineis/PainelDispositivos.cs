using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Dispositivos: status (porta respondendo + latência) de cada aparelho; o clique abre as ações do adb para ele,
// sempre com a confirmação das Ações rápidas.
public static class PainelDispositivos
{
    public static UIElement Criar(Config config, Estilo estilo, Acoes acoes)
    {
        var cfg = config.Dispositivos;
        if (cfg.Lista.Count == 0)
            return new TextBlock
            {
                Text = "Nenhum aparelho. Adicione em Configurações → Dispositivos.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontFamily = estilo.Fonte,
            };

        var lista = new StackPanel { Spacing = 2 };
        var linhas = cfg.Lista.Select(d => Linha(d, config, estilo, acoes)).ToList();
        foreach (var (botao, _) in linhas) lista.Children.Add(botao);

        void Atualizar()
        {
            foreach (var (_, atualizar) in linhas) atualizar();
        }

        // "há 20 s" precisa andar sozinho; o status chega pelo serviço.
        var relogio = lista.DispatcherQueue.CreateTimer();
        relogio.Interval = TimeSpan.FromSeconds(10);
        relogio.Tick += (_, _) => Atualizar();
        lista.Loaded += (_, _) =>
        {
            ServicoDispositivos.Mudou += Atualizar;
            ServicoDispositivos.Acompanhar(() => config.Dispositivos);
            relogio.Start();
            Atualizar();
        };
        lista.Unloaded += (_, _) =>
        {
            ServicoDispositivos.Mudou -= Atualizar;
            relogio.Stop();
        };
        return lista;
    }

    static (Button Botao, Action Atualizar) Linha(Dispositivo d, Config config, Estilo estilo, Acoes acoes)
    {
        var icone = new TextBlock
        {
            Text = d.Icone,
            FontFamily = new FontFamily("Segoe UI Emoji"),
            FontSize = 24,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var nome = new TextBlock { Text = d.Nome, FontFamily = estilo.Fonte, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        var detalhe = new TextBlock { FontFamily = estilo.Fonte, FontSize = 12, Opacity = 0.7, TextTrimming = TextTrimming.CharacterEllipsis };
        var textos = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Children = { nome, detalhe } };

        var bolinha = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 10, Height = 10, VerticalAlignment = VerticalAlignment.Center };
        var latencia = new TextBlock { FontFamily = estilo.Fonte, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, MinWidth = 64, TextAlignment = TextAlignment.Right };
        var status = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center, Children = { latencia, bolinha } };

        var grade = new Grid { ColumnSpacing = 12 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textos, 1);
        Grid.SetColumn(status, 2);
        grade.Children.Add(icone);
        grade.Children.Add(textos);
        grade.Children.Add(status);

        var botao = new Button
        {
            Content = grade,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 6, 8, 6),
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.5)),
        };
        ToolTipService.SetToolTip(botao, $"{d.Alvo} — clique para as ações (conectar, espelhar, print, logs, instalar APK)");
        botao.Flyout = Menu(d, config, acoes, botao);

        void Atualizar()
        {
            var s = ServicoDispositivos.De(d);
            var testando = ServicoDispositivos.EmTeste(d);
            Windows.UI.Color cor;
            if (s is null) { cor = Microsoft.UI.Colors.Gray; latencia.Text = testando ? "testando…" : ""; }
            else if (s.Respondeu) { cor = Windows.UI.Color.FromArgb(255, 61, 190, 139); latencia.Text = $"{s.Ms} ms"; }
            else { cor = Windows.UI.Color.FromArgb(255, 229, 72, 77); latencia.Text = "sem resposta"; }
            bolinha.Fill = new SolidColorBrush(cor);
            bolinha.Opacity = testando ? 0.4 : 1;
            detalhe.Text = s is null ? d.Alvo : $"{d.Alvo}  ·  {Relativo(s.Quando)}";
        }
        return (botao, Atualizar);
    }

    static MenuFlyout Menu(Dispositivo d, Config config, Acoes acoes, Button botao)
    {
        var menu = new MenuFlyout();
        foreach (var acao in d.Acoes())
        {
            var item = new MenuFlyoutItem
            {
                Text = acao.Nome,
                Icon = new FontIcon { Glyph = acao.Icone, FontFamily = new FontFamily("Segoe UI Emoji") },
            };
            ToolTipService.SetToolTip(item, acao.Descricao);
            item.Click += async (_, _) => await PainelAcoes.Executar(acao, config.Projetos, acoes, botao.XamlRoot);
            menu.Items.Add(item);
        }
        menu.Items.Add(new MenuFlyoutSeparator());
        var testar = new MenuFlyoutItem { Text = "Testar agora", Icon = new FontIcon { Glyph = "" } };
        testar.Click += (_, _) => _ = ServicoDispositivos.Testar(d);
        var copiar = new MenuFlyoutItem { Text = $"Copiar endereço ({d.Alvo})", Icon = new FontIcon { Glyph = "" } };
        copiar.Click += (_, _) =>
        {
            var pacote = new DataPackage();
            pacote.SetText(d.Alvo);
            Clipboard.SetContent(pacote);
        };
        menu.Items.Add(testar);
        menu.Items.Add(copiar);
        return menu;
    }

    static string Relativo(DateTime quando)
    {
        var d = DateTime.Now - quando;
        return d.TotalSeconds < 10 ? "agora"
            : d.TotalMinutes < 1 ? $"há {(int)d.TotalSeconds} s"
            : d.TotalHours < 1 ? $"há {(int)d.TotalMinutes} min"
            : $"às {quando:HH:mm}";
    }
}
