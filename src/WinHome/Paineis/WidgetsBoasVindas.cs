using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Cartões do lado direito do Boas-vindas. Cada um mostra "…" até os dados chegarem e
// expõe Recarregar (chamado periodicamente; usa o cache enquanto ele valer).
public sealed class Widget
{
    public required UIElement Elemento { get; init; }
    public required Func<Task> Recarregar { get; init; }
    public Action? AtualizarHora { get; init; } // relógio local da cidade, chamado a cada segundo
}

public static class WidgetsBoasVindas
{
    static readonly CultureInfo PtBr = new("pt-BR");

    static Border Cartao(Estilo estilo, UIElement conteudo, double largura) => new()
    {
        Background = estilo.FundoPainel,
        CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.75)),
        Padding = new Thickness(14, 12, 14, 12),
        Width = largura,
        VerticalAlignment = VerticalAlignment.Top,
        Child = conteudo,
    };

    static TextBlock Texto(Estilo estilo, double tamanho, double opacidade = 1, bool negrito = false) => new()
    {
        FontFamily = estilo.Fonte,
        FontSize = tamanho,
        Opacity = opacidade,
        FontWeight = negrito ? FontWeights.SemiBold : FontWeights.Normal,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };

    public static Widget Clima(Cidade cidade, string unidade, Estilo estilo)
    {
        var nome = Texto(estilo, estilo.Corpo, negrito: true);
        nome.Text = cidade.Nome;
        ToolTipService.SetToolTip(nome, ServicoClima.Descrever(cidade));
        var hora = Texto(estilo, estilo.Legenda, 0.7);
        var icone = new TextBlock { FontSize = estilo.Tam(28), VerticalAlignment = VerticalAlignment.Center, Text = "…" };
        var temperatura = Texto(estilo, estilo.Tam(32));
        temperatura.FontWeight = FontWeights.Light;
        temperatura.VerticalAlignment = VerticalAlignment.Center;
        var descricao = Texto(estilo, estilo.Legenda, 0.8);
        var extremos = Texto(estilo, estilo.Legenda, 0.8);

        TimeZoneInfo? fuso = null;
        try { if (cidade.Fuso is { Length: > 0 } id) fuso = TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { }

        void AtualizarHora()
        {
            if (fuso is not null) hora.Text = TimeZoneInfo.ConvertTime(DateTime.Now, fuso).ToString("HH:mm", PtBr);
        }
        AtualizarHora();

        async Task Recarregar()
        {
            var r = await ServicoClima.Obter(cidade, unidade);
            if (r is null)
            {
                icone.Text = "⚠️";
                descricao.Text = "Sem conexão";
                return;
            }
            var p = r.Valor;
            (icone.Text, descricao.Text) = ServicoClima.Tempo(p.Codigo, p.Dia);
            temperatura.Text = $"{Math.Round(p.Temperatura):0}°";
            extremos.Text = $"Máx {Math.Round(p.Maxima):0}° · Mín {Math.Round(p.Minima):0}°";
            ToolTipService.SetToolTip(temperatura, r.Atual
                ? $"Atualizado às {r.Quando:HH:mm}"
                : $"Sem conexão — último dado às {r.Quando:HH:mm}");
            descricao.Opacity = r.Atual ? 0.8 : 0.5;
        }

        var linha = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { icone, temperatura } };
        var conteudo = new StackPanel { Spacing = 2, Children = { nome, hora, linha, descricao, extremos } };
        return new Widget { Elemento = Cartao(estilo, conteudo, 150), Recarregar = Recarregar, AtualizarHora = AtualizarHora };
    }

    // Selo de alerta: só aparece enquanto algum aparelho conectado (e visível) estiver abaixo do limite "baixa".
    public static Widget Baterias(Baterias cfg, Estilo estilo)
    {
        var vermelho = new SolidColorBrush(PainelBaterias.Cor(0, cfg));
        var titulo = Texto(estilo, estilo.Corpo, negrito: true);
        titulo.Text = "Bateria baixa";
        titulo.Foreground = vermelho;
        var cabeca = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { new FontIcon { Glyph = PainelBaterias.GlifoDaBateria(10), FontSize = estilo.Realce, Foreground = vermelho }, titulo },
        };
        var linhas = new StackPanel { Spacing = 4 };
        var cartao = Cartao(estilo, new StackPanel { Spacing = 8, Children = { cabeca, linhas } }, 170);
        cartao.Visibility = Visibility.Collapsed;

        void Atualizar()
        {
            var baixas = ServicoBaterias.Atuais
                .Where(b => b.Conectado && cfg.Visivel(b.Nome) && ServicoBaterias.FaixaDe(b.Nivel, cfg) == ServicoBaterias.Faixa.Baixa)
                .ToList();
            cartao.Visibility = baixas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            linhas.Children.Clear();
            foreach (var b in baixas)
            {
                var nome = Texto(estilo, estilo.Corpo);
                nome.Text = cfg.NomeDe(b.Nome);
                var nivel = Texto(estilo, estilo.Corpo, negrito: true);
                nivel.Text = $"{b.Nivel}%";
                nivel.Foreground = vermelho;
                var grade = new Grid { ColumnSpacing = 8 };
                grade.ColumnDefinitions.Add(new ColumnDefinition());
                grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(nivel, 1);
                grade.Children.Add(nome);
                grade.Children.Add(nivel);
                linhas.Children.Add(grade);
            }
            ToolTipService.SetToolTip(cartao, $"Abaixo de {cfg.Baixa}%. Hora de carregar.");
        }

        cartao.Loaded += (_, _) =>
        {
            ServicoBaterias.Mudou += Atualizar;
            Atualizar();
        };
        cartao.Unloaded += (_, _) => ServicoBaterias.Mudou -= Atualizar;
        return new Widget
        {
            Elemento = cartao,
            Recarregar = () =>
            {
                Atualizar();
                return Task.CompletedTask;
            },
        };
    }

    public static Widget Cotacoes(IReadOnlyList<string> moedas, Estilo estilo)
    {
        var titulo = Texto(estilo, estilo.Corpo, negrito: true);
        titulo.Text = "Cotações";
        var linhas = new StackPanel { Spacing = 6 };
        var rodape = Texto(estilo, estilo.Legenda, 0.6);
        rodape.Text = "…";

        var verde = new SolidColorBrush(Color.FromArgb(255, 0x3D, 0xBE, 0x8B));
        var vermelho = new SolidColorBrush(Color.FromArgb(255, 0xF0, 0x5D, 0x5D));

        async Task Recarregar()
        {
            var r = await ServicoCotacoes.Obter(moedas);
            linhas.Children.Clear();
            if (r is null || r.Valor.Count == 0)
            {
                rodape.Text = "Sem conexão";
                return;
            }
            foreach (var c in r.Valor)
            {
                var codigo = Texto(estilo, estilo.Corpo, negrito: true);
                codigo.Text = c.Moeda;
                ToolTipService.SetToolTip(codigo, ServicoCotacoes.Nome(c.Moeda));
                var valor = Texto(estilo, estilo.Corpo);
                valor.Text = c.Valor.ToString(c.Valor >= 1000 ? "C0" : "C2", PtBr);
                var variacao = Texto(estilo, estilo.Legenda);
                if (c.Variacao is { } pct) // sem variação = a API mandou um valor implausível (ServicoCotacoes)
                {
                    variacao.Text = $"{(pct >= 0 ? "▲" : "▼")} {Math.Abs(pct).ToString("0.00", PtBr)}%";
                    variacao.Foreground = pct >= 0 ? verde : vermelho;
                }

                var grade = new Grid { ColumnSpacing = 8 };
                grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                grade.ColumnDefinitions.Add(new ColumnDefinition());
                grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(valor, 1);
                Grid.SetColumn(variacao, 2);
                grade.Children.Add(codigo);
                grade.Children.Add(valor);
                grade.Children.Add(variacao);
                linhas.Children.Add(grade);
            }
            var horario = r.Valor.Max(c => c.Horario);
            rodape.Text = r.Atual ? $"Cotação de {horario:dd/MM HH:mm}" : $"Sem conexão — dados de {horario:dd/MM HH:mm}";
        }

        var conteudo = new StackPanel { Spacing = 8, Children = { titulo, linhas, rodape } };
        return new Widget { Elemento = Cartao(estilo, conteudo, 210), Recarregar = Recarregar };
    }
}
