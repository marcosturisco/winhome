using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Monta a Home a partir da config: painéis visíveis, na ordem da lista, em linhas de "colunas" colunas.
public static class HomeBuilder
{
    public static UIElement Montar(Config config, Estilo estilo, Acoes acoes)
    {
        var linhas = new StackPanel { Spacing = estilo.Espaco };
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

            var cartao = Cartao(painel.Titulo, Conteudo(painel, config, estilo, acoes), estilo);
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

    static UIElement Conteudo(Painel painel, Config config, Estilo estilo, Acoes acoes)
    {
        if (painel.Id == Config.PainelBoasVindas) return PainelBoasVindas.Criar(config.Nome, config.BoasVindas, estilo);
        if (painel.Id == Config.PainelMensagem) return PainelMensagem.Criar(config.Mensagens, estilo);
        if (painel.Id == Config.PainelCuriosidade) return PainelCuriosidade.Criar(config.Curiosidades, estilo);
        if (painel.Id == Config.PainelProjetos) return PainelProjetos.Criar(config.Projetos, estilo, acoes);
        if (config.Grupos.TryGetValue(painel.Id, out var apps)) return PainelApps.Criar(apps, painel.Id, estilo, acoes);
        return new TextBlock
        {
            Text = $"Painel \"{painel.Id}\" não existe ainda e não há grupo com esse nome em \"grupos\".",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            FontFamily = estilo.Fonte,
        };
    }

    static Grid NovaLinha(Estilo estilo)
    {
        var grid = new Grid { ColumnSpacing = estilo.Espaco };
        for (int i = 0; i < estilo.Colunas; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grid;
    }

    static Border Cartao(string? titulo, UIElement conteudo, Estilo estilo)
    {
        var pilha = new StackPanel { Spacing = 12 };
        if (!string.IsNullOrWhiteSpace(titulo))
        {
            pilha.Children.Add(new TextBlock
            {
                Text = titulo,
                FontFamily = estilo.Fonte,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = estilo.Destaque,
            });
        }
        pilha.Children.Add(conteudo);

        return new Border
        {
            Background = estilo.FundoPainel,
            CornerRadius = estilo.Cantos,
            Padding = new Thickness(20),
            Child = pilha,
        };
    }
}
