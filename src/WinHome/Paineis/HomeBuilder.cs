using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Monta a Home a partir da config: painéis visíveis, na ordem da lista, em linhas de "colunas" colunas.
public static class HomeBuilder
{
    public static UIElement Montar(Config config, Estilo estilo, Acoes acoes)
    {
        var linhas = new StackPanel { Spacing = estilo.Espaco };
        var arrasto = new ArrastoPaineis(linhas, acoes.Editor);
        Grid? linha = null;
        int usado = 0;

        foreach (var painel in config.Paineis.Where(p => p.Visivel))
        {
            var largura = Math.Clamp(painel.Largura, 1, estilo.Colunas);
            if (linha is null || usado + largura > estilo.Colunas)
            {
                linha = NovaLinha(estilo);
                linhas.Children.Add(linha);
                usado = 0;
            }

            var (conteudo, acao) = Conteudo(painel, config, estilo, acoes);
            var cartao = CartaoDoPainel(painel, conteudo, acao, estilo, acoes, linha, usado, arrasto);
            Grid.SetColumn(cartao, usado);
            Grid.SetColumnSpan(cartao, largura);
            linha.Children.Add(cartao);
            usado += largura;
        }

        return linhas;
    }

    static readonly CompareInfo Comparador = new CultureInfo("pt-BR").CompareInfo;

    static bool Casa(string nome, string termo) =>
        Comparador.IndexOf(nome, termo, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    // Resultados da busca: apps dos grupos e, depois, os demais apps instalados.
    // "botoes" recebe os botões na ordem, para o Enter abrir o primeiro e a seta para baixo focar.
    public static UIElement MontarResultados(Config config, string termo, IReadOnlyList<AppInstalado> instalados,
        Estilo estilo, Acoes acoes, List<Button> botoes)
    {
        var pilha = new StackPanel { Spacing = estilo.Espaco };

        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (grupo, apps) in config.Grupos)
        {
            var achados = apps.Where(a => Casa(a.Nome, termo) && vistos.Add(a.App ?? a.Caminho ?? a.Nome)).ToList();
            if (achados.Count > 0)
                pilha.Children.Add(Cartao(acoes.Editor.Titulo(grupo),
                    PainelApps.Criar(achados, grupo, estilo, acoes, botoes), estilo));
        }

        var outros = instalados.Where(a => !vistos.Contains(a.AppId) && Casa(a.Nome, termo))
            .Take(40).Select(a => a.ParaItem()).ToList();
        if (outros.Count > 0)
            pilha.Children.Add(Cartao("Outros apps instalados", PainelApps.Criar(outros, null, estilo, acoes, botoes), estilo));

        if (pilha.Children.Count == 0)
            pilha.Children.Add(Cartao(null, new TextBlock
            {
                Text = $"Nada encontrado para \"{termo}\".",
                FontFamily = estilo.Fonte,
                Opacity = 0.7,
            }, estilo));
        return pilha;
    }

    // Conteúdo do painel e, opcionalmente, um botão para o cabeçalho (ex.: ↻ das mensagens e curiosidades).
    static (UIElement Conteudo, Button? Acao) Conteudo(Painel painel, Config config, Estilo estilo, Acoes acoes)
    {
        if (painel.Id == Config.PainelBoasVindas) return (PainelBoasVindas.Criar(config.Nome, config.BoasVindas, estilo), null);
        if (painel.Id == Config.PainelMensagem)
        {
            var outra = BotaoCabecalho("", "Outra mensagem");
            return (PainelMensagem.Criar(config.Mensagens, estilo, outra), outra);
        }
        if (painel.Id == Config.PainelCuriosidade)
        {
            var outra = BotaoCabecalho("", "Outra curiosidade");
            return (PainelCuriosidade.Criar(config.Curiosidades, estilo, outra), outra);
        }
        if (painel.Id == Config.PainelProjetos) return (PainelProjetos.Criar(config.Projetos, estilo, acoes), null);
        if (painel.Id == Config.PainelTarefas) return (PainelTarefas.Criar(config.Tarefas, estilo), null);
        if (painel.Id == Config.PainelAcoes) return (PainelAcoes.Criar(config, estilo, acoes), null);
        if (painel.Id == Config.PainelVideos) return (PainelVideos.Criar(config.Videos, estilo, acoes), null);
        if (painel.Id == Config.PainelDispositivos) return (PainelDispositivos.Criar(config, estilo, acoes), null);
        if (painel.Id == Config.PainelAgenda)
        {
            var atualizar = BotaoCabecalho("", "Atualizar a agenda agora");
            return (PainelAgenda.Criar(config.Agenda, estilo, atualizar), atualizar);
        }
        if (config.AppsAncorados.TryGetValue(painel.Id, out var ancorado))
            return (PainelAncorado.Criar(painel.Id, ancorado, config, estilo, acoes), null);
        if (config.Grupos.TryGetValue(painel.Id, out var apps)) return (PainelApps.Criar(apps, painel.Id, estilo, acoes), null);
        return (new TextBlock
        {
            Text = $"Painel \"{painel.Id}\" não existe ainda e não há grupo com esse nome em \"grupos\".",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            FontFamily = estilo.Fonte,
        }, null);
    }

    // Botão discreto do canto do cabeçalho: lugar fixo, que não depende do tamanho do conteúdo.
    static Button BotaoCabecalho(string glifo, string dica)
    {
        var botao = new Button
        {
            Content = new FontIcon { Glyph = glifo, FontSize = 14 },
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Width = 36,
            Height = 36,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(botao, dica);
        AutomationProperties.SetName(botao, dica);
        return botao;
    }

    static Grid NovaLinha(Estilo estilo)
    {
        var grid = new Grid { ColumnSpacing = estilo.Espaco };
        for (int i = 0; i < estilo.Colunas; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grid;
    }

    // ferramentas = botões do canto direito do cabeçalho (ex.: ↻ e ✕); sem título, ficam por cima, no canto.
    static FrameworkElement Cartao(string? titulo, UIElement conteudo, Estilo estilo, FrameworkElement? ferramentas = null)
    {
        var pilha = new StackPanel { Spacing = 12 };
        var temTitulo = !string.IsNullOrWhiteSpace(titulo);
        if (temTitulo)
        {
            var cabecalho = new Grid();
            cabecalho.ColumnDefinitions.Add(new ColumnDefinition());
            cabecalho.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cabecalho.Children.Add(new TextBlock
            {
                Text = titulo,
                FontFamily = estilo.Fonte,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = estilo.Destaque,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            });
            if (ferramentas is not null)
            {
                ferramentas.Margin = new Thickness(0, -8, -10, -8); // não aumenta a altura do cabeçalho
                Grid.SetColumn(ferramentas, 1);
                cabecalho.Children.Add(ferramentas);
            }
            pilha.Children.Add(cabecalho);
        }
        pilha.Children.Add(conteudo);

        var borda = new Border
        {
            Background = estilo.FundoPainel,
            CornerRadius = estilo.Cantos,
            Padding = new Thickness(20),
            Child = pilha,
        };
        if (temTitulo || ferramentas is null) return borda;

        ferramentas.HorizontalAlignment = HorizontalAlignment.Right;
        ferramentas.VerticalAlignment = VerticalAlignment.Top;
        ferramentas.Margin = new Thickness(0, 10, 10, 0);
        return new Grid { Children = { borda, ferramentas } };
    }

    // Cartão de um painel da Home: ✥ muda de lugar, ✕ oculta (com "Desfazer") e a borda direita ajusta a largura.
    static FrameworkElement CartaoDoPainel(Painel painel, UIElement conteudo, Button? acao, Estilo estilo, Acoes acoes,
        Grid linha, int coluna, ArrastoPaineis arrasto)
    {
        var titulo = painel.Titulo is { Length: > 0 } t ? t
            : EditorConfig.Especiais.FirstOrDefault(e => e.Id == painel.Id).Nome ?? painel.Id;
        var fechar = BotaoCabecalho("", "Ocultar este painel (volta em Configurações → Painéis)");
        fechar.Opacity = 0; // aparece ao passar o mouse; o espaço fica reservado para o ↻ não mudar de lugar
        fechar.Click += (_, _) =>
        {
            acoes.Editor.DefinirVisivel(painel, false);
            acoes.Desfazer($"\"{titulo}\" foi ocultado.", () => acoes.Editor.DefinirVisivel(painel, true));
        };
        var ferramentas = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        if (acao is not null)
        {
            acao.Margin = new Thickness(0);
            ferramentas.Children.Add(acao);
        }
        var mover = new AlcaMover(painel, arrasto) { Opacity = 0 };
        ferramentas.Children.Add(mover);
        ferramentas.Children.Add(fechar);

        var cartao = Cartao(painel.Titulo, conteudo, estilo, ferramentas);
        var alca = new AlcaLargura(painel, estilo, acoes, linha, coluna);
        // Barras que mostram onde o painel arrastado vai cair: no vão entre os cartões, antes ou depois deste.
        Border Indicador(HorizontalAlignment lado) => new()
        {
            Width = 4,
            CornerRadius = new CornerRadius(2),
            Background = estilo.Destaque,
            HorizontalAlignment = lado,
            Margin = lado == HorizontalAlignment.Left
                ? new Thickness(-(estilo.Espaco / 2) - 2, 0, 0, 0)
                : new Thickness(0, 0, -(estilo.Espaco / 2) - 2, 0),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
        };
        var antes = Indicador(HorizontalAlignment.Left);
        var depois = Indicador(HorizontalAlignment.Right);
        var raiz = new Grid { Children = { cartao, alca, antes, depois } };
        arrasto.Registrar(raiz, painel, antes, depois);

        void Ferramentas(bool ver)
        {
            fechar.Opacity = ver ? 0.8 : 0;
            mover.Opacity = ver ? 0.8 : 0;
            alca.Destacar(ver);
        }
        raiz.PointerEntered += (_, _) => Ferramentas(true);
        raiz.PointerExited += (_, _) => Ferramentas(false);
        fechar.GotFocus += (_, _) => fechar.Opacity = 0.8; // acessível pelo teclado
        return raiz;
    }
}
