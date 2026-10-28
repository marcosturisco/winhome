using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// App ancorado: a área do painel recebe a janela real do app (Ancoragem), que acompanha a Home.
// A base do painel tem uma alça para mudar a altura; ✥ ↔ ✕ do cartão mudam lugar, largura e ocultam.
public static class PainelAncorado
{
    // alturaFixa: a linha da Home tem altura definida; a área fica com o espaço que sobra (sem a alça de altura própria).
    public static UIElement Criar(string id, AppAncorado app, Config config, Estilo estilo, Acoes acoes, bool alturaFixa = false)
    {
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Opacity = 0.75, FontFamily = estilo.Fonte };
        var abrir = new Button { Content = $"Abrir {app.Nome} aqui", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var pegar = new Button { Content = "Ancorar a janela que já está aberta" };
        var conteudo = new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { status, abrir, pegar },
        };
        var area = new Border
        {
            Height = alturaFixa ? double.NaN : Math.Clamp(app.Altura, 200, 2000),
            MinHeight = alturaFixa ? 60 : 0,
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.5)),
            BorderBrush = estilo.Destaque,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(estilo.Escuro ? Windows.UI.Color.FromArgb(40, 255, 255, 255) : Windows.UI.Color.FromArgb(20, 0, 0, 0)),
            Child = conteudo,
        };
        Ancoragem.Registrar(id, area);

        var soltar = new Button { Content = "Soltar", Padding = new Thickness(10, 4, 10, 4), FontSize = 12 };
        ToolTipService.SetToolTip(soltar, "Devolve a janela ao tamanho e lugar de antes");
        var titulo = new TextBlock { FontSize = 12, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, FontFamily = estilo.Fonte };
        var rodape = new Grid { ColumnSpacing = 8 };
        rodape.ColumnDefinitions.Add(new ColumnDefinition());
        rodape.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(soltar, 1);
        rodape.Children.Add(titulo);
        rodape.Children.Add(soltar);

        var processo = NomeDoProcesso(app);
        void Atualizar()
        {
            var ancorado = Ancoragem.Ancorado(id);
            conteudo.Visibility = ancorado ? Visibility.Collapsed : Visibility.Visible;
            soltar.Visibility = ancorado ? Visibility.Visible : Visibility.Collapsed;
            titulo.Text = ancorado ? Ancoragem.Titulo(Ancoragem.Janela(id)) : "";
            pegar.Visibility = processo is null ? Visibility.Collapsed : Visibility.Visible; // sem processo, só abrindo
            if (!ancorado && status.Text.Length == 0)
                status.Text = $"{app.Nome} vai aparecer nesta área e acompanhar a Home.";
        }

        bool Ancorar()
        {
            if (processo is null) return false;
            var janela = Ancoragem.Procurar(processo);
            return janela != IntPtr.Zero && Ancoragem.Ancorar(id, janela);
        }

        pegar.Click += (_, _) =>
        {
            if (!Ancorar()) status.Text = $"Nenhuma janela de {app.Nome} aberta (e livre) foi encontrada.";
            Atualizar();
        };
        abrir.Click += async (_, _) =>
        {
            if (Ancorar()) { Atualizar(); return; } // já estava aberto
            var antes = Ancoragem.Janelas().Select(j => j.Janela).ToHashSet(); // processo desconhecido: vale a janela nova
            if (Abrir(app, config) is { } erro) { status.Text = erro; return; }
            abrir.IsEnabled = pegar.IsEnabled = false;
            status.Text = $"Abrindo {app.Nome}…";
            // Espera a janela principal aparecer (o IntelliJ leva alguns segundos).
            for (var i = 0; i < 120 && !Ancoragem.Ancorado(id); i++)
            {
                await Task.Delay(500);
                if (Ancorar()) break;
                if (processo is null && Ancoragem.Nova(antes) is { } nova && Ancoragem.Ancorar(id, nova.Janela))
                {
                    acoes.Editor.DefinirProcessoAncorado(id, nova.Processo); // da próxima vez já sabe qual procurar
                    break;
                }
            }
            abrir.IsEnabled = pegar.IsEnabled = true;
            if (!Ancoragem.Ancorado(id)) status.Text = $"A janela de {app.Nome} não apareceu em 1 minuto. Tente \"Ancorar a janela que já está aberta\".";
            Atualizar();
        };
        soltar.Click += (_, _) =>
        {
            Ancoragem.Soltar(id);
            status.Text = "";
            Atualizar();
        };

        void Mudou(string qual)
        {
            if (qual != id) return;
            if (!Ancoragem.Ancorado(id)) status.Text = "";
            Atualizar();
        }
        Ancoragem.Mudou += Mudou;
        area.Unloaded += (_, _) => Ancoragem.Mudou -= Mudou;
        Atualizar();

        if (!alturaFixa)
        {
            var alca = new AlcaAlturaArea(area, novaAltura => acoes.Editor.DefinirAlturaAncorado(id, novaAltura));
            return new StackPanel { Spacing = 6, Children = { area, alca, rodape } };
        }
        var painel = new Grid { RowSpacing = 6, Children = { area, rodape } };
        painel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // a área do app
        painel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(rodape, 1);
        return painel;
    }

    // Processo dono da janela: o informado na config ou, para os editores, o executável deles.
    static string? NomeDoProcesso(AppAncorado app)
    {
        if (!string.IsNullOrWhiteSpace(app.Processo)) return app.Processo;
        if (app.Editor is { Length: > 0 } editor)
            return editor switch { "intellij" => "idea64", "androidStudio" => "studio64", "vscode" => "Code", _ => null };
        return app.Caminho is { Length: > 0 } c ? Path.GetFileNameWithoutExtension(c) : null;
    }

    static string? Abrir(AppAncorado app, Config config)
    {
        if (app.Editor is { Length: > 0 } editor)
            return ServicoProjetos.Executavel(editor, config.Projetos.Editores) is { } exe
                ? Lancador.Abrir(new AppItem { Nome = app.Nome, Caminho = exe, Args = app.Args })
                : $"Não encontrei o {app.Nome}. Informe o caminho em Configurações → Projetos.";
        return Lancador.Abrir(new AppItem { Nome = app.Nome, App = app.App, Caminho = app.Caminho, Args = app.Args });
    }
}

// Alça na base do painel: arrastar muda a altura da área do app (grava ao soltar).
public sealed class AlcaAlturaArea : Grid
{
    double? _inicioY, _alturaInicial;

    public AlcaAlturaArea(FrameworkElement area, Action<int> gravar)
    {
        Height = 10;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        ToolTipService.SetToolTip(this, "Arraste para mudar a altura");
        Children.Add(new Border
        {
            Width = 48,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            _inicioY = e.GetCurrentPoint(null).Position.Y;
            _alturaInicial = area.ActualHeight;
            CapturePointer(e.Pointer);
            e.Handled = true;
        };
        PointerMoved += (_, e) =>
        {
            if (_inicioY is not { } y0 || _alturaInicial is not { } h0) return;
            area.Height = Math.Clamp(h0 + e.GetCurrentPoint(null).Position.Y - y0, 200, 2000);
        };
        PointerReleased += (_, e) =>
        {
            if (_inicioY is null) return;
            _inicioY = null;
            ReleasePointerCaptures();
            gravar((int)area.Height);
        };
        PointerCaptureLost += (_, _) => _inicioY = null;
    }
}
