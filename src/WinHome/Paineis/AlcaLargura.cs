using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Borda direita de um painel: arrastar muda a largura encaixando nas colunas da grade (mostra "4 de 6 colunas")
// e grava "largura" ao soltar; duplo clique faz o painel ocupar o espaço livre da linha.
// É uma classe própria só para poder trocar o cursor (ProtectedCursor).
public sealed class AlcaLargura : Grid
{
    readonly Painel _painel;
    readonly Estilo _estilo;
    readonly Acoes _acoes;
    readonly Grid _linha;
    readonly int _coluna;
    readonly Border _traco;
    readonly Border _selo;
    readonly TextBlock _texto;
    int? _arrastando; // largura em colunas durante o arrasto

    public AlcaLargura(Painel painel, Estilo estilo, Acoes acoes, Grid linha, int coluna)
    {
        _painel = painel;
        _estilo = estilo;
        _acoes = acoes;
        _linha = linha;
        _coluna = coluna;

        Width = 12;
        HorizontalAlignment = HorizontalAlignment.Right;
        Margin = new Thickness(0, 0, -6, 0); // metade fora: pega a borda sem cobrir o conteúdo
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        ToolTipService.SetToolTip(this, "Arraste para mudar a largura (em colunas); duplo clique ocupa o espaço livre da linha");

        _traco = new Border
        {
            Width = 4,
            Height = 36,
            CornerRadius = new CornerRadius(2),
            Background = estilo.Destaque,
            Opacity = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Children.Add(_traco);

        // Selo "4 de 6 colunas", mostrado no meio do painel durante o arrasto (fica no cartão, não na alça).
        _texto = new TextBlock { FontFamily = estilo.Fonte, FontSize = 14, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
        _selo = new Border
        {
            Background = estilo.Destaque,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 4, 12, 4),
            Child = _texto,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
        };
        Loaded += (_, _) =>
        {
            if (Parent is Grid cartao && !cartao.Children.Contains(_selo)) cartao.Children.Add(_selo);
        };

        PointerPressed += Pressionado;
        PointerMoved += Movido;
        PointerReleased += Solto;
        PointerCaptureLost += (_, _) => Terminar(gravar: false);
        DoubleTapped += (_, e) =>
        {
            e.Handled = true;
            var livre = _estilo.Colunas - OcupadoPelosOutros();
            if (livre > 0 && livre != Largura) _acoes.Editor.DefinirLargura(_painel, livre);
        };
    }

    int Largura => Grid.GetColumnSpan((FrameworkElement)Parent);

    public void Destacar(bool sim)
    {
        if (_arrastando is null) _traco.Opacity = sim ? 0.6 : 0;
    }

    // Colunas usadas pelos outros painéis desta linha.
    int OcupadoPelosOutros() =>
        _linha.Children.OfType<FrameworkElement>().Where(e => e != Parent).Sum(Grid.GetColumnSpan);

    void Pressionado(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _arrastando = Largura;
        CapturePointer(e.Pointer);
        _traco.Opacity = 1;
        Mostrar(_arrastando.Value);
        e.Handled = true;
    }

    void Movido(object sender, PointerRoutedEventArgs e)
    {
        if (_arrastando is null) return;
        var colunas = _estilo.Colunas;
        var espaco = _estilo.Espaco;
        var larguraColuna = (_linha.ActualWidth - espaco * (colunas - 1)) / colunas;
        if (larguraColuna <= 0) return;

        // Da borda esquerda do painel até o ponteiro, arredondado para a coluna mais próxima.
        var x = e.GetCurrentPoint(_linha).Position.X - _coluna * (larguraColuna + espaco);
        var nova = Math.Clamp((int)Math.Round((x + espaco) / (larguraColuna + espaco)), 1, colunas);
        if (nova == _arrastando) return;
        _arrastando = nova;
        // Prévia dentro da linha atual; o que não couber desce para a linha seguinte ao soltar.
        Grid.SetColumnSpan((FrameworkElement)Parent, Math.Min(nova, colunas - _coluna));
        Mostrar(nova);
    }

    void Solto(object sender, PointerRoutedEventArgs e)
    {
        Terminar(gravar: true);
        ReleasePointerCaptures();
        e.Handled = true;
    }

    void Terminar(bool gravar)
    {
        if (_arrastando is not { } nova) return;
        _arrastando = null;
        _selo.Visibility = Visibility.Collapsed;
        _traco.Opacity = 0;
        if (gravar && nova != _painel.Largura) _acoes.Editor.DefinirLargura(_painel, nova); // remonta a Home
        else Grid.SetColumnSpan((FrameworkElement)Parent, Math.Min(_painel.Largura, _estilo.Colunas - _coluna));
    }

    void Mostrar(int colunas)
    {
        _texto.Text = $"{colunas} de {_estilo.Colunas} colunas";
        _selo.Visibility = Visibility.Visible;
    }
}
