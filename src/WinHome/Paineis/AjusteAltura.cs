using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace WinHome.Paineis;

// Conteúdo de um painel numa linha de altura fixa: encolhe por igual até caber (sem cortar informação).
// O conteúdo é medido mais largo na mesma proporção, então continua ocupando a largura toda (ícones e textos
// se rearranjam). Abaixo da escala mínima fica ilegível: aí para de encolher e a rolagem de fora assume.
public sealed class AjusteAltura : Panel
{
    const double EscalaMinima = 0.5;
    readonly ScaleTransform _escala = new();
    double _alturaVisivel; // altura da rolagem de fora (o espaço que o painel tem)

    AjusteAltura(UIElement conteudo)
    {
        conteudo.RenderTransform = _escala;
        Children.Add(conteudo);
    }

    public static ScrollViewer Envolver(UIElement conteudo)
    {
        var ajuste = new AjusteAltura(conteudo);
        var rolagem = new ScrollViewer { Content = ajuste, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        rolagem.SizeChanged += (_, e) =>
        {
            if (Math.Abs(e.NewSize.Height - ajuste._alturaVisivel) < 0.5) return;
            ajuste._alturaVisivel = e.NewSize.Height;
            ajuste.InvalidateMeasure();
        };
        return rolagem;
    }

    protected override Size MeasureOverride(Size disponivel)
    {
        var conteudo = Children[0];
        var largura = disponivel.Width;
        var altura = _alturaVisivel;
        if (altura <= 0 || double.IsInfinity(largura))
        {
            _escala.ScaleX = _escala.ScaleY = 1;
            conteudo.Measure(disponivel);
            return conteudo.DesiredSize;
        }

        // Altura ocupada com a escala s: mede com largura/s e reduz o resultado por s.
        double Ocupa(double s)
        {
            conteudo.Measure(new Size(largura / s, double.PositiveInfinity));
            return conteudo.DesiredSize.Height * s;
        }

        double escala;
        if (Ocupa(1) <= altura) escala = 1;
        else if (Ocupa(EscalaMinima) > altura) escala = EscalaMinima;
        else
        {
            double cabe = EscalaMinima, naoCabe = 1;
            for (var i = 0; i < 8; i++)
            {
                var meio = (cabe + naoCabe) / 2;
                if (Ocupa(meio) <= altura) cabe = meio; else naoCabe = meio;
            }
            escala = cabe;
            Ocupa(escala); // a última medida tem de ser a escolhida
        }
        _escala.ScaleX = _escala.ScaleY = escala;
        return new Size(largura, conteudo.DesiredSize.Height * escala);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var conteudo = Children[0];
        var s = _escala.ScaleX;
        conteudo.Arrange(new Rect(0, 0, final.Width / s, conteudo.DesiredSize.Height));
        return final;
    }
}
