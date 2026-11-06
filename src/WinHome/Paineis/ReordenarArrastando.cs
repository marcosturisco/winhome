using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace WinHome.Paineis;

// Reordenar os itens de um painel (StackPanel, grade de ícones…) arrastando com o mouse: depois de alguns pixels
// o item segue o ponteiro de lugar em lugar (os outros abrem espaço); soltar grava a nova ordem. Um clique sem
// arrastar continua sendo um clique normal (abrir o app, marcar a tarefa). Esc ou perder o ponteiro desfaz.
// O item precisa ter Tag; podeTrocar(arrastado, alvo) limita onde ele pode cair (ex.: só entre pendentes).
// aoSoltar(arrastado, anterior, seguinte) recebe as Tags vizinhas na nova posição (null = início/fim).
public static class ReordenarArrastando
{
    const double Limiar = 8; // pixels antes de virar arrasto

    public static void Ativar(Panel painel, Func<object, object, bool> podeTrocar,
        Action<object, object?, object?> aoSoltar)
    {
        UIElement? item = null;      // item pressionado
        Point inicio = default;
        bool arrastando = false;
        int origem = -1;

        void Encerrar(bool gravar)
        {
            var arrastado = item;
            var estava = arrastando;
            item = null;
            arrastando = false;
            if (!estava || arrastado is not FrameworkElement fe) return;
            fe.Opacity = 1;
            painel.ReleasePointerCaptures();
            var i = painel.Children.IndexOf(arrastado);
            if (!gravar)
            {
                if (i != origem && i >= 0 && origem >= 0)
                {
                    painel.Children.RemoveAt(i);
                    painel.Children.Insert(Math.Min(origem, painel.Children.Count), arrastado);
                }
                return;
            }
            if (i == origem) return;
            object? Vizinho(int j) => j >= 0 && j < painel.Children.Count && painel.Children[j] is FrameworkElement { Tag: { } t } ? t : null;
            aoSoltar(fe.Tag!, Vizinho(i - 1), Vizinho(i + 1));
        }

        // handledEventsToo: botões e caixas de marcar já tratam o clique e não deixariam o evento chegar aqui.
        painel.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) =>
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse) return;
            if (!e.GetCurrentPoint(painel).Properties.IsLeftButtonPressed) return;
            item = ItemDe(painel, e.OriginalSource as DependencyObject);
            arrastando = false;
            inicio = e.GetCurrentPoint(painel).Position;
        }), true);

        painel.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((_, e) =>
        {
            if (item is not FrameworkElement { Tag: not null } fe) return;
            if (!e.GetCurrentPoint(painel).Properties.IsLeftButtonPressed) { Encerrar(false); return; }
            var p = e.GetCurrentPoint(painel).Position;
            if (!arrastando)
            {
                if (Math.Abs(p.X - inicio.X) < Limiar && Math.Abs(p.Y - inicio.Y) < Limiar) return;
                // Tira a captura do botão/caixa (assim o soltar não vira clique) e passa para o painel, que não sai do lugar.
                for (var d = e.OriginalSource as DependencyObject; d is not null && d != painel; d = VisualTreeHelper.GetParent(d))
                    if (d is UIElement u) u.ReleasePointerCaptures();
                if (!painel.CapturePointer(e.Pointer)) { item = null; return; }
                arrastando = true;
                origem = painel.Children.IndexOf(fe);
                fe.Opacity = 0.5;
            }
            e.Handled = true;

            // Item sob o ponteiro: o arrastado passa a ocupar o lugar dele.
            for (var j = 0; j < painel.Children.Count; j++)
            {
                if (painel.Children[j] is not FrameworkElement { Tag: { } alvo } outro || outro == fe) continue;
                var r = outro.TransformToVisual(painel).TransformBounds(new Rect(0, 0, outro.ActualWidth, outro.ActualHeight));
                if (!r.Contains(p) || !podeTrocar(fe.Tag, alvo)) continue;
                painel.Children.Remove(fe);
                painel.Children.Insert(j, fe);
                break;
            }
        }), true);

        painel.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, e) =>
        {
            if (arrastando) e.Handled = true;
            Encerrar(true);
        }), true);
        // Só a captura do próprio painel durante o arrasto: a do botão (que tiramos ao começar) também sobe até aqui.
        painel.PointerCaptureLost += (_, e) =>
        {
            if (arrastando && ReferenceEquals(e.OriginalSource, painel)) Encerrar(false);
        };
        painel.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) =>
        {
            if (!arrastando || e.Key != Windows.System.VirtualKey.Escape) return;
            e.Handled = true; // o Esc desfaz o arrasto em vez de esconder a Home
            Encerrar(false);
        }), true);
    }

    // Filho direto do painel que contém o elemento clicado (dentro de uma caixa de texto, não: lá arrastar seleciona).
    static UIElement? ItemDe(Panel painel, DependencyObject? d)
    {
        while (d is not null)
        {
            if (d is TextBox) return null;
            var pai = VisualTreeHelper.GetParent(d);
            if (pai == painel) return d as UIElement;
            d = pai;
        }
        return null;
    }
}
