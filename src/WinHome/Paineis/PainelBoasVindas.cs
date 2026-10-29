using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Painel do topo: saudação, hora e data à esquerda; alerta de bateria baixa, clima e cotações à direita.
// O relógio atualiza a cada segundo e os cartões a cada minuto (usando o cache enquanto ele vale).
public static class PainelBoasVindas
{
    static readonly CultureInfo PtBr = new("pt-BR");

    public static UIElement Criar(string nome, BoasVindas opcoes, Baterias baterias, Estilo estilo)
    {
        var saudacao = new TextBlock { FontFamily = estilo.Fonte, FontSize = estilo.Grande, Opacity = 0.85 };
        var hora = new TextBlock
        {
            FontFamily = estilo.Fonte,
            FontSize = estilo.Tam(72),
            FontWeight = FontWeights.SemiLight,
            Foreground = estilo.Destaque,
        };
        var data = new TextBlock { FontFamily = estilo.Fonte, FontSize = estilo.Grande, Opacity = 0.85 };
        var formatoHora = opcoes.MostrarSegundos ? "HH:mm:ss" : "HH:mm";

        var widgets = new List<Widget>();
        if (baterias.AlertaNoBoasVindas) widgets.Add(WidgetsBoasVindas.Baterias(baterias, estilo)); // à esquerda do clima
        if (opcoes.Clima.Visivel)
            widgets.AddRange(opcoes.Clima.Cidades.Select(c => WidgetsBoasVindas.Clima(c, opcoes.Clima.Unidade, estilo)));
        if (opcoes.Cotacoes.Visivel && opcoes.Cotacoes.Moedas.Count > 0)
            widgets.Add(WidgetsBoasVindas.Cotacoes(opcoes.Cotacoes.Moedas, estilo));

        void Atualizar()
        {
            var agora = DateTime.Now;
            var periodo = agora.Hour switch { < 12 => "Bom dia", < 18 => "Boa tarde", _ => "Boa noite" };
            saudacao.Text = string.IsNullOrWhiteSpace(nome) ? periodo : $"{periodo}, {nome}";
            hora.Text = agora.ToString(formatoHora, PtBr);
            var texto = agora.ToString("dddd, d 'de' MMMM 'de' yyyy", PtBr);
            data.Text = char.ToUpper(texto[0], PtBr) + texto[1..];
            foreach (var w in widgets) w.AtualizarHora?.Invoke();
        }

        var pilha = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        if (opcoes.MostrarSaudacao) pilha.Children.Add(saudacao);
        pilha.Children.Add(hora);
        if (opcoes.MostrarData) pilha.Children.Add(data);

        var cartoes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        foreach (var w in widgets) cartoes.Children.Add(w.Elemento);

        var grade = new Grid { ColumnSpacing = 24 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(cartoes, 1);
        grade.Children.Add(pilha);
        grade.Children.Add(cartoes);

        var fila = DispatcherQueue.GetForCurrentThread();
        var relogio = fila.CreateTimer();
        relogio.Interval = TimeSpan.FromSeconds(1);
        relogio.Tick += (_, _) => Atualizar();
        var dados = fila.CreateTimer();
        dados.Interval = TimeSpan.FromMinutes(1);
        dados.Tick += (_, _) => Recarregar();

        void Recarregar()
        {
            foreach (var w in widgets) _ = w.Recarregar();
        }

        grade.Loaded += (_, _) =>
        {
            Atualizar();
            relogio.Start();
            dados.Start();
            Recarregar();
        };
        grade.Unloaded += (_, _) =>
        {
            relogio.Stop();
            dados.Stop();
        };
        Atualizar();
        return grade;
    }
}
