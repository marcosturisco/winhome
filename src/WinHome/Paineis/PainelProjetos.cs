using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Projetos recentes: clique abre no editor padrão (ou na IDE de origem); botão direito abre em outro lugar,
// fixa no topo ou oculta.
public static class PainelProjetos
{
    public static UIElement Criar(Projetos cfg, Estilo estilo, Acoes acoes)
    {
        var lista = new StackPanel { Spacing = 2 };
        lista.Children.Add(new TextBlock { Text = "Carregando…", Opacity = 0.7, FontFamily = estilo.Fonte });

        async void Carregar()
        {
            var todos = await ServicoProjetos.Listar(cfg);
            var fixados = cfg.Fixados.Select(ServicoProjetos.Chave).ToList();
            var ocultos = cfg.Ocultos.Select(ServicoProjetos.Chave).ToHashSet();
            var exibir = todos
                .Where(p => !ocultos.Contains(ServicoProjetos.Chave(p.Caminho)))
                .OrderBy(p =>
                {
                    var i = fixados.IndexOf(ServicoProjetos.Chave(p.Caminho));
                    return i < 0 ? int.MaxValue : i; // fixados primeiro, na ordem em que foram fixados
                })
                .Take(Math.Clamp(cfg.Maximo, 1, 50))
                .ToList();

            lista.Children.Clear();
            if (exibir.Count == 0)
            {
                lista.Children.Add(new TextBlock
                {
                    Text = "Nenhum projeto encontrado. Confira as pastas abaixo e as fontes em Configurações → Projetos.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.7,
                    FontFamily = estilo.Fonte,
                });
                return;
            }
            foreach (var p in exibir)
                lista.Children.Add(Linha(p, fixados.Contains(ServicoProjetos.Chave(p.Caminho)), cfg, estilo, acoes));
        }

        lista.Loaded += (_, _) => Carregar();
        return new StackPanel { Spacing = 8, Children = { lista, BotaoPastas(cfg, estilo, acoes) } };
    }

    // Rodapé: pastas cujas subpastas com git entram na lista; trocar, remover ou adicionar sem abrir as Configurações.
    static Button BotaoPastas(Projetos cfg, Estilo estilo, Acoes acoes)
    {
        var flyout = new Flyout();
        var conteudo = new StackPanel { Spacing = 6, MinWidth = 320, MaxWidth = 520 };
        conteudo.Children.Add(new TextBlock
        {
            Text = "Pastas com projetos (cada subpasta com git vira um projeto)",
            FontFamily = estilo.Fonte,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8,
        });

        async void Escolher(string? antiga)
        {
            flyout.Hide();
            if (await acoes.EscolherPasta() is not { } nova) return;
            if (antiga is null) acoes.Editor.AdicionarPastaProjetos(nova);
            else acoes.Editor.TrocarPastaProjetos(antiga, nova);
        }

        Button Icone(string glifo, string dica, Action acao)
        {
            var b = new Button
            {
                Content = new FontIcon { Glyph = glifo, FontSize = estilo.Legenda },
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6),
                VerticalAlignment = VerticalAlignment.Center,
            };
            ToolTipService.SetToolTip(b, dica);
            b.Click += (_, _) => acao();
            return b;
        }

        if (cfg.Pastas.Count == 0)
            conteudo.Children.Add(new TextBlock { Text = "Nenhuma pasta.", Opacity = 0.6, FontFamily = estilo.Fonte });
        foreach (var pasta in cfg.Pastas)
        {
            var nome = new TextBlock
            {
                Text = pasta,
                FontFamily = estilo.Fonte,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            ToolTipService.SetToolTip(nome, pasta);
            var trocar = Icone("", "Trocar por outra pasta…", () => Escolher(pasta));
            var remover = Icone("", "Remover pasta", () =>
            {
                flyout.Hide();
                acoes.Editor.RemoverPastaProjetos(pasta);
            });

            var linha = new Grid { ColumnSpacing = 4 };
            linha.ColumnDefinitions.Add(new ColumnDefinition());
            linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(trocar, 1);
            Grid.SetColumn(remover, 2);
            linha.Children.Add(nome);
            linha.Children.Add(trocar);
            linha.Children.Add(remover);
            conteudo.Children.Add(linha);
        }

        var adicionar = new Button { Content = "+ Adicionar pasta…", Margin = new Thickness(0, 4, 0, 0) };
        adicionar.Click += (_, _) => Escolher(null);
        conteudo.Children.Add(adicionar);
        flyout.Content = conteudo;

        var botao = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "", FontSize = estilo.Legenda },
                    new TextBlock { Text = "Pastas…", FontFamily = estilo.Fonte, FontSize = estilo.Legenda },
                },
            },
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Flyout = flyout,
        };
        ToolTipService.SetToolTip(botao, "Trocar ou adicionar as pastas de projetos");
        return botao;
    }

    static Button Linha(Projeto p, bool fixado, Projetos cfg, Estilo estilo, Acoes acoes)
    {
        var destinoPadrao = cfg.EditorPadrao == "origem" ? (p.Origem == "pasta" ? "vscode" : p.Origem) : cfg.EditorPadrao;

        UIElement icone = ServicoProjetos.Executavel(p.Origem == "pasta" ? destinoPadrao : p.Origem, cfg.Editores) is { } exe &&
                          Icones.De(new AppItem { Caminho = exe }, 24) is { } img
            ? new Image { Source = img, Width = 24, Height = 24 }
            : new FontIcon { Glyph = "", FontSize = estilo.Tam(20) };
        if (icone is FrameworkElement fe) fe.VerticalAlignment = VerticalAlignment.Center;

        var nome = new TextBlock
        {
            Text = (fixado ? "📌 " : "") + p.Nome,
            FontFamily = estilo.Fonte,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var detalhes = new TextBlock
        {
            Text = string.Join("  ·  ", new[]
            {
                p.Branch is { } b ? $"⎇ {b}" : null,
                p.Wsl ? "WSL" : null,
                p.Quando is { } q ? Relativo(q) : null,
            }.Where(s => s is not null)),
            FontFamily = estilo.Fonte,
            FontSize = estilo.Legenda,
            Opacity = 0.7,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var textos = new StackPanel { Spacing = 1, Children = { nome, detalhes } };

        var grade = new Grid { ColumnSpacing = 12 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(textos, 1);
        grade.Children.Add(icone);
        grade.Children.Add(textos);

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
        ToolTipService.SetToolTip(botao, p.Caminho);
        botao.Click += (_, _) => acoes.Resultado(ServicoProjetos.Abrir(p, destinoPadrao, cfg.Editores));

        var menu = new MenuFlyout();
        foreach (var (id, nomeDestino) in ServicoProjetos.Destinos)
        {
            var item = new MenuFlyoutItem { Text = $"Abrir no {nomeDestino}" };
            item.Click += (_, _) => acoes.Resultado(ServicoProjetos.Abrir(p, id, cfg.Editores));
            menu.Items.Add(item);
        }
        menu.Items.Add(new MenuFlyoutSeparator());
        var fixar = new MenuFlyoutItem { Text = fixado ? "Desafixar" : "Fixar no topo", Icon = new FontIcon { Glyph = "" } };
        fixar.Click += (_, _) => acoes.Editor.FixarProjeto(p.Caminho, !fixado);
        var ocultar = new MenuFlyoutItem { Text = "Ocultar da lista", Icon = new FontIcon { Glyph = "" } };
        ocultar.Click += (_, _) => acoes.Editor.OcultarProjeto(p.Caminho, true);
        menu.Items.Add(fixar);
        menu.Items.Add(ocultar);
        botao.ContextFlyout = menu;
        return botao;
    }

    static readonly CultureInfo PtBr = new("pt-BR");

    static string Relativo(DateTime quando)
    {
        var d = DateTime.Now - quando;
        if (d.TotalMinutes < 60) return "agora há pouco";
        if (d.TotalHours < 24 && quando.Date == DateTime.Today) return $"hoje, {quando:HH:mm}";
        if (quando.Date == DateTime.Today.AddDays(-1)) return "ontem";
        if (d.TotalDays < 7) return $"há {(int)Math.Ceiling(d.TotalDays)} dias";
        return quando.ToString("dd/MM/yyyy", PtBr);
    }
}
