using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Os painéis de uma montagem da Home, para o arrasto saber onde cada um está e mostrar onde o painel vai cair.
public sealed class ArrastoPaineis(FrameworkElement raiz, EditorConfig editor)
{
    sealed record Item(FrameworkElement Cartao, Painel Painel, Border Antes, Border Depois);

    readonly List<Item> _itens = [];
    public EditorConfig Editor => editor;
    public IReadOnlyList<Painel> Visiveis => _itens.Select(i => i.Painel).ToList();

    public void Registrar(FrameworkElement cartao, Painel painel, Border antes, Border depois) =>
        _itens.Add(new(cartao, painel, antes, depois));

    // Painel mais próximo do ponteiro e se o arrastado cai antes ou depois dele (pela metade horizontal).
    public (Painel Alvo, bool Depois)? Alvo(Point ponteiro, Painel arrastado)
    {
        Item? melhor = null;
        Rect melhorRet = default;
        var menor = double.MaxValue;
        foreach (var item in _itens)
        {
            var r = item.Cartao.TransformToVisual(raiz).TransformBounds(new Rect(0, 0, item.Cartao.ActualWidth, item.Cartao.ActualHeight));
            var dx = Math.Max(0, Math.Max(r.Left - ponteiro.X, ponteiro.X - r.Right));
            var dy = Math.Max(0, Math.Max(r.Top - ponteiro.Y, ponteiro.Y - r.Bottom));
            var d = dx * dx + dy * dy;
            if (d < menor) { menor = d; melhor = item; melhorRet = r; }
        }
        if (melhor is null || melhor.Painel == arrastado) return null;
        return (melhor.Painel, ponteiro.X > melhorRet.Left + melhorRet.Width / 2);
    }

    public Point Posicao(PointerRoutedEventArgs e) => e.GetCurrentPoint(raiz).Position;

    public void Indicar((Painel Alvo, bool Depois)? alvo)
    {
        foreach (var item in _itens)
        {
            item.Antes.Visibility = alvo is { } a && a.Alvo == item.Painel && !a.Depois ? Visibility.Visible : Visibility.Collapsed;
            item.Depois.Visibility = alvo is { } b && b.Alvo == item.Painel && b.Depois ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public FrameworkElement? CartaoDe(Painel painel) => _itens.FirstOrDefault(i => i.Painel == painel)?.Cartao;
}

// Alça ✥ do cabeçalho: arrastar reposiciona o painel (barra de destaque mostra onde cai); botão direito tem
// "para o início / para trás / para frente / para o fim". Classe própria só para trocar o cursor.
public sealed class AlcaMover : Grid
{
    readonly Painel _painel;
    readonly ArrastoPaineis _arrasto;
    bool _arrastando;
    (Painel Alvo, bool Depois)? _alvo;

    public AlcaMover(Painel painel, ArrastoPaineis arrasto)
    {
        _painel = painel;
        _arrasto = arrasto;
        Width = 36;
        Height = 36;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
        Children.Add(new FontIcon { Glyph = "", FontSize = 14 });
        ToolTipService.SetToolTip(this, "Arraste para mudar o painel de lugar (botão direito: mais opções)");

        PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            _arrastando = true;
            _alvo = null;
            CapturePointer(e.Pointer);
            if (_arrasto.CartaoDe(_painel) is { } c) c.Opacity = 0.5;
            e.Handled = true;
        };
        PointerMoved += (_, e) =>
        {
            if (!_arrastando) return;
            _alvo = _arrasto.Alvo(_arrasto.Posicao(e), _painel);
            _arrasto.Indicar(_alvo);
        };
        PointerReleased += (_, e) =>
        {
            var alvo = _alvo;
            Terminar();
            ReleasePointerCaptures();
            e.Handled = true;
            if (alvo is { } a) _arrasto.Editor.MoverPainelPara(_painel, a.Alvo, a.Depois); // remonta a Home
        };
        PointerCaptureLost += (_, _) => Terminar();

        var menu = new MenuFlyout();
        void Item(string texto, string glifo, Func<(Painel, bool)?> destino)
        {
            var item = new MenuFlyoutItem { Text = texto, Icon = new FontIcon { Glyph = glifo } };
            item.Click += (_, _) =>
            {
                if (destino() is { } d) _arrasto.Editor.MoverPainelPara(_painel, d.Item1, d.Item2);
            };
            menu.Items.Add(item);
        }
        Item("Mover para o início", "", () => Vizinho(int.MinValue));
        Item("Mover para trás", "", () => Vizinho(-1));
        Item("Mover para frente", "", () => Vizinho(+1));
        Item("Mover para o fim", "", () => Vizinho(int.MaxValue));
        ContextFlyout = menu;
    }

    // Destino entre os painéis visíveis: antes do anterior / depois do seguinte / antes do primeiro / depois do último.
    (Painel, bool)? Vizinho(int passo)
    {
        var lista = _arrasto.Visiveis;
        var i = lista.ToList().IndexOf(_painel);
        if (i < 0 || lista.Count < 2) return null;
        return passo switch
        {
            int.MinValue => i == 0 ? null : (lista[0], false),
            int.MaxValue => i == lista.Count - 1 ? null : (lista[^1], true),
            < 0 => i == 0 ? null : (lista[i - 1], false),
            _ => i == lista.Count - 1 ? null : (lista[i + 1], true),
        };
    }

    void Terminar()
    {
        if (!_arrastando) return;
        _arrastando = false;
        _arrasto.Indicar(null);
        if (_arrasto.CartaoDe(_painel) is { } c) c.Opacity = 1;
    }
}
