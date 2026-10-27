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
        var nome = Texto(estilo, 14, negrito: true);
        nome.Text = cidade.Nome;
        ToolTipService.SetToolTip(nome, ServicoClima.Descrever(cidade));
        var hora = Texto(estilo, 12, 0.7);
        var icone = new TextBlock { FontSize = 28, VerticalAlignment = VerticalAlignment.Center, Text = "…" };
        var temperatura = Texto(estilo, 32);
        temperatura.FontWeight = FontWeights.Light;
        temperatura.VerticalAlignment = VerticalAlignment.Center;
        var descricao = Texto(estilo, 12, 0.8);
        var extremos = Texto(estilo, 12, 0.8);

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

    public static Widget Cotacoes(IReadOnlyList<string> moedas, Estilo estilo)
    {
        var titulo = Texto(estilo, 14, negrito: true);
        titulo.Text = "Cotações";
        var linhas = new StackPanel { Spacing = 6 };
        var rodape = Texto(estilo, 11, 0.6);
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
                var codigo = Texto(estilo, 13, negrito: true);
                codigo.Text = c.Moeda;
                ToolTipService.SetToolTip(codigo, ServicoCotacoes.Nome(c.Moeda));
                var valor = Texto(estilo, 13);
                valor.Text = c.Valor.ToString(c.Valor >= 1000 ? "C0" : "C2", PtBr);
                var variacao = Texto(estilo, 12);
                variacao.Text = $"{(c.Variacao >= 0 ? "▲" : "▼")} {Math.Abs(c.Variacao).ToString("0.00", PtBr)}%";
                variacao.Foreground = c.Variacao >= 0 ? verde : vermelho;

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
