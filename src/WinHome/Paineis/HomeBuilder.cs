using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Monta a Home a partir da config: painéis visíveis, na ordem da lista, em linhas de "colunas" colunas.
public static class HomeBuilder
{
    public static UIElement Montar(Config config, Estilo estilo, Action<AppItem> abrir)
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

            var cartao = Cartao(painel, Conteudo(painel, config, estilo, abrir), estilo);
            Grid.SetColumn(cartao, usado);
            Grid.SetColumnSpan(cartao, largura);
            linha.Children.Add(cartao);
            usado += largura;
        }

        return linhas;
    }

    static UIElement Conteudo(Painel painel, Config config, Estilo estilo, Action<AppItem> abrir)
    {
        if (painel.Id == "relogio") return PainelRelogio.Criar(config.Nome, estilo);
        if (config.Grupos.TryGetValue(painel.Id, out var apps)) return PainelApps.Criar(apps, estilo, abrir);
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

    static Border Cartao(Painel painel, UIElement conteudo, Estilo estilo)
    {
        var pilha = new StackPanel { Spacing = 12 };
        if (!string.IsNullOrWhiteSpace(painel.Titulo))
        {
            pilha.Children.Add(new TextBlock
            {
                Text = painel.Titulo,
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
