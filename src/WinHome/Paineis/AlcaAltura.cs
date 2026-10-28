using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Borda de baixo de um painel: arrastar muda a altura da linha inteira (todos os painéis dela), de 10 em 10 pixels,
// e grava "altura" nos painéis da linha ao soltar; duplo clique volta para a altura automática (pelo conteúdo).
// Não deixa a linha ficar mais baixa que a altura mínima de um app ancorado nela.
// É uma classe própria só para poder trocar o cursor (ProtectedCursor).
public sealed class AlcaAltura : Grid
{
    readonly IReadOnlyList<Painel> _paineis;
    readonly Acoes _acoes;
    readonly Grid _linha;
    readonly Border _traco;
    readonly Border _selo;
    readonly TextBlock _texto;
    int? _arrastando; // altura da linha durante o arrasto
    double _alturaAntes; // Height da linha antes do arrasto (NaN = automática)

    public AlcaAltura(IReadOnlyList<Painel> paineis, Estilo estilo, Acoes acoes, Grid linha)
    {
        _paineis = paineis;
        _acoes = acoes;
        _linha = linha;

        Height = 12;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(0, 0, 0, -6); // metade fora: pega a borda sem cobrir o conteúdo
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        ToolTipService.SetToolTip(this, "Arraste para mudar a altura da linha (todos os painéis dela); duplo clique volta ao automático");

        _traco = new Border
        {
            Width = 36,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = estilo.Destaque,
            Opacity = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Children.Add(_traco);

        // Selo "Linha: 320 px", mostrado no meio do painel durante o arrasto (fica no cartão, não na alça).
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
            if (_paineis.Any(p => p.Altura != 0)) _acoes.Editor.DefinirAlturaLinha(_paineis, 0);
        };
    }

    public void Destacar(bool sim)
    {
        if (_arrastando is null) _traco.Opacity = sim ? 0.6 : 0;
    }

    void Pressionado(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _alturaAntes = _linha.Height;
        _arrastando = (int)Math.Round(_linha.ActualHeight);
        CapturePointer(e.Pointer);
        _traco.Opacity = 1;
        Mostrar(_arrastando.Value);
        e.Handled = true;
    }

    void Movido(object sender, PointerRoutedEventArgs e)
    {
        if (_arrastando is null) return;
        // Do topo da linha até o ponteiro, de 10 em 10 pixels.
        var y = e.GetCurrentPoint(_linha).Position.Y;
        var nova = (int)Math.Clamp(Math.Round(y / 10) * 10, Minima(), Painel.AlturaMaxima);
        if (nova == _arrastando) return;
        _arrastando = nova;
        _linha.Height = nova; // prévia: o conteúdo já encolhe para caber (AjusteAltura)
        Mostrar(nova);
    }

    // Menor altura da linha: a geral ou a que deixa um app ancorado dela na altura mínima que ele aceita.
    int Minima()
    {
        var minima = (double)Painel.AlturaMinima;
        foreach (var p in _paineis)
            if (Servicos.Ancoragem.FaltaNaArea(p.Id) is { } falta)
                minima = Math.Max(minima, Math.Ceiling((_linha.ActualHeight + falta) / 10) * 10);
        return (int)Math.Min(minima, Painel.AlturaMaxima);
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
        var mudou = double.IsNaN(_alturaAntes) ? !double.IsNaN(_linha.Height) : nova != (int)_alturaAntes; // só clicar não fixa
        if (gravar && mudou) _acoes.Editor.DefinirAlturaLinha(_paineis, nova); // remonta a Home
        else _linha.Height = _alturaAntes;
    }

    void Mostrar(int altura)
    {
        _texto.Text = $"Linha: {altura} px";
        _selo.Visibility = Visibility.Visible;
    }
}
