using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// O que os painéis podem fazer: abrir apps, editar a config pelo menu de contexto, informar o resultado
// de uma abertura feita por eles mesmos (null = abriu, a Home pode se esconder; texto = erro a mostrar)
// pedir uma pasta ou um arquivo ao usuário (null = cancelou; tipos ex.: [".apk"] ou ["*"]) e mostrar uma
// informação que não é erro (ex.: saída de um comando), com um botão "Desfazer" opcional.
public sealed record Acoes(Action<AppItem> Abrir, EditorConfig Editor, Action<string?> Resultado, Func<Task<string?>> EscolherPasta,
    Action<string> Informar, Func<string[], Task<string?>> EscolherArquivo, Action<string, Action> Desfazer);

// Grade de botões de apps; navegável por setas, Enter abre, botão direito mostra o menu.
public static class PainelApps
{
    // grupo = id do grupo dono dos apps; null = apps fora dos grupos (resultados da busca).
    public static UIElement Criar(IEnumerable<AppItem> apps, string? grupo, Estilo estilo, Acoes acoes,
        List<Button>? botoes = null)
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
        {
            var botao = Botao(app, estilo, acoes);
            botao.ContextFlyout = grupo is null ? MenuDeFora(app, acoes) : MenuDoGrupo(app, grupo, acoes);
            grade.Children.Add(botao);
            botoes?.Add(botao);
        }

        if (grade.Children.Count == 0)
            return new TextBlock
            {
                Text = "Nenhum app neste grupo. Use a busca ou as Configurações para adicionar.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontFamily = estilo.Fonte,
            };
        return grade;
    }

    static MenuFlyout MenuDoGrupo(AppItem app, string grupo, Acoes acoes)
    {
        var ed = acoes.Editor;
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Abrir", "", () => acoes.Abrir(app)));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Mover para a esquerda", "", () => ed.MoverApp(grupo, app, -1)));
        menu.Items.Add(Item("Mover para a direita", "", () => ed.MoverApp(grupo, app, +1)));

        var outros = ed.Grupos().Where(g => g.Id != grupo).ToList();
        if (outros.Count > 0)
        {
            var sub = new MenuFlyoutSubItem { Text = "Mover para o grupo", Icon = new FontIcon { Glyph = "" } };
            foreach (var (id, titulo) in outros)
                sub.Items.Add(Item(titulo, null, () => ed.MoverAppPara(grupo, app, id)));
            menu.Items.Add(sub);
        }
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Remover do grupo", "", () => ed.RemoverApp(grupo, app)));
        return menu;
    }

    static MenuFlyout MenuDeFora(AppItem app, Acoes acoes)
    {
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Abrir", "", () => acoes.Abrir(app)));
        var sub = new MenuFlyoutSubItem { Text = "Adicionar ao grupo", Icon = new FontIcon { Glyph = "" } };
        foreach (var (id, titulo) in acoes.Editor.Grupos())
            sub.Items.Add(Item(titulo, null, () => acoes.Editor.AdicionarApp(id, Copia(app))));
        if (sub.Items.Count > 0) menu.Items.Add(sub);
        return menu;
    }

    static AppItem Copia(AppItem a) => new()
    {
        Nome = a.Nome, App = a.App, Caminho = a.Caminho, Args = a.Args, Pasta = a.Pasta, Icone = a.Icone,
    };

    static MenuFlyoutItem Item(string texto, string? glifo, Action acao)
    {
        var item = new MenuFlyoutItem { Text = texto };
        if (glifo is not null) item.Icon = new FontIcon { Glyph = glifo };
        item.Click += (_, _) => acao();
        return item;
    }

    static Button Botao(AppItem app, Estilo estilo, Acoes acoes)
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
            Tag = app,
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
        botao.Click += (_, _) => acoes.Abrir(app);
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
