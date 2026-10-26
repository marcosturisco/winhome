using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinHome.Paineis;

// Saudação, hora e data; atualiza a cada segundo enquanto estiver na tela.
public static class PainelRelogio
{
    static readonly CultureInfo PtBr = new("pt-BR");

    public static UIElement Criar(string nome, Estilo estilo)
    {
        var saudacao = new TextBlock { FontFamily = estilo.Fonte, FontSize = 20, Opacity = 0.85 };
        var hora = new TextBlock
        {
            FontFamily = estilo.Fonte,
            FontSize = 72,
            FontWeight = FontWeights.SemiLight,
            Foreground = estilo.Destaque,
        };
        var data = new TextBlock { FontFamily = estilo.Fonte, FontSize = 20, Opacity = 0.85 };

        void Atualizar()
        {
            var agora = DateTime.Now;
            var periodo = agora.Hour switch { < 12 => "Bom dia", < 18 => "Boa tarde", _ => "Boa noite" };
            saudacao.Text = string.IsNullOrWhiteSpace(nome) ? periodo : $"{periodo}, {nome}";
            hora.Text = agora.ToString("HH:mm", PtBr);
            var texto = agora.ToString("dddd, d 'de' MMMM 'de' yyyy", PtBr);
            data.Text = char.ToUpper(texto[0], PtBr) + texto[1..];
        }

        var pilha = new StackPanel { Spacing = 2, Children = { saudacao, hora, data } };

        var relogio = DispatcherQueue.GetForCurrentThread().CreateTimer();
        relogio.Interval = TimeSpan.FromSeconds(1);
        relogio.Tick += (_, _) => Atualizar();
        pilha.Loaded += (_, _) => { Atualizar(); relogio.Start(); };
        pilha.Unloaded += (_, _) => relogio.Stop();
        Atualizar();
        return pilha;
    }
}
