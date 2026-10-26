using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using WinHome.Configuracao;
using WinHome.Paineis;
using WinHome.Servicos;

namespace WinHome;

// Janela da Home: sem bordas, ocupa a área de trabalho (ou a tela toda) e é montada a partir da config.
public sealed class MainWindow : Window
{
    readonly IntPtr _hwnd;
    readonly Grid _raiz = new();
    readonly ScrollViewer _rolagem = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    readonly InfoBar _aviso = new() { Severity = InfoBarSeverity.Warning, IsClosable = true, Margin = new Thickness(16, 16, 16, 0) };
    readonly ConfigService _config;
    readonly AtalhoGlobal _atalho;
    bool _saindo;

    public MainWindow()
    {
        Title = "WinHome";
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        _raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _raiz.RowDefinitions.Add(new RowDefinition());
        Grid.SetRow(_rolagem, 1);
        var barra = BarraDeBotoes();
        Grid.SetRow(barra, 1);
        _raiz.Children.Add(_aviso);
        _raiz.Children.Add(_rolagem);
        _raiz.Children.Add(barra);
        _raiz.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(TeclaPressionada), true);
        Content = _raiz;

        _atalho = new AtalhoGlobal(_hwnd);
        _atalho.Pressionado += Alternar;

        // Fechar (Alt+F4) só esconde; sair de verdade é pelo botão de desligar.
        AppWindow.Closing += (_, e) =>
        {            if (_saindo) return;
            e.Cancel = true;
            Esconder();
        };
        Closed += (_, _) =>
        {
            _atalho.Dispose();
            _config?.Dispose();
        };

        _config = new ConfigService(DispatcherQueue);
        _config.Mudou += Aplicar;
        var erro = _config.Carregar(); // antes de ler Atual
        Aplicar(_config.Atual, erro);
    }

    void Aplicar(Config config, string? erroConfig)
    {
        var erros = new List<string>();
        if (erroConfig is not null) erros.Add(erroConfig + " (mantida a configuração anterior)");

        var tema = config.Tema;
        _raiz.RequestedTheme = tema.Modo switch
        {
            "claro" => ElementTheme.Light,
            "escuro" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
        var escuro = _raiz.RequestedTheme switch
        {
            ElementTheme.Dark => true,
            ElementTheme.Light => false,
            _ => Application.Current.RequestedTheme == ApplicationTheme.Dark,
        };
        var estilo = new Estilo { Tema = tema, Escuro = escuro };

        AplicarFundo(tema, erros);
        AjustarJanela(config.Janela);

        if (_atalho.Definir(config.Atalho) is { } erroAtalho) erros.Add(erroAtalho);
        if (Inicializacao.Sincronizar(config.IniciarComWindows) is { } erroInicio) erros.Add(erroInicio);

        var margem = Math.Max(24, estilo.Espaco * 3);
        _rolagem.Padding = new Thickness(margem, margem + 24, margem, margem);
        _rolagem.Content = HomeBuilder.Montar(config, estilo, AbrirApp);
        MostrarAviso(erros);
    }

    void AplicarFundo(Tema tema, List<string> erros)
    {
        _raiz.Background = new SolidColorBrush(Colors.Transparent);
        switch (tema.Fundo)
        {
            case "micaAlt":
                SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                break;
            case "acrilico":
                SystemBackdrop = new DesktopAcrylicBackdrop();
                break;
            case "cor":
                SystemBackdrop = null;
                _raiz.Background = new SolidColorBrush(Estilo.Cor(tema.CorFundo, Colors.Black));
                break;
            case "imagem":
                SystemBackdrop = null;
                var arquivo = Environment.ExpandEnvironmentVariables(tema.ImagemFundo ?? "");
                if (File.Exists(arquivo))
                    _raiz.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(arquivo)), Stretch = Stretch.UniformToFill };
                else
                    erros.Add($"Imagem de fundo não encontrada: \"{tema.ImagemFundo}\".");
                break;
            default:
                SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
                break;
        }
    }

    void AjustarJanela(string modo)
    {
        if (modo == "telaCheia")
        {
            if (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen)
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            return;
        }

        if (AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            presenter = (OverlappedPresenter)AppWindow.Presenter;
        }
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        // Área de trabalho do monitor onde a janela está: preserva a barra de tarefas.
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.MoveAndResize(area);
    }

    FrameworkElement BarraDeBotoes()
    {
        Button Botao(string glifo, string dica, Action acao)
        {
            var b = new Button
            {
                Content = new FontIcon { Glyph = glifo, FontSize = 14 },
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10),
            };
            ToolTipService.SetToolTip(b, dica);
            b.Click += (_, _) => acao();
            return b;
        }

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12),
            Spacing = 4,
            Children =
            {
                Botao("", "Editar configuração (config.json)", AbrirConfig),
                Botao("", "Esconder (Esc)", Esconder),
                Botao("", "Sair do WinHome", Sair),
            },
        };
    }

    void AbrirApp(AppItem item)
    {
        if (Lancador.Abrir(item) is { } erro) MostrarAviso([erro]);
        else if (_config.Atual.EsconderAoAbrirApp) Esconder();
    }

    void AbrirConfig()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ConfigService.Arquivo) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Sem programa associado a .json: usa o Bloco de Notas.
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{ConfigService.Arquivo}\"") { UseShellExecute = true });
        }
    }

    void MostrarAviso(List<string> erros)
    {
        _aviso.Title = erros.Count > 0 ? "Atenção" : "";
        _aviso.Message = string.Join("\n", erros);
        _aviso.IsOpen = erros.Count > 0;
    }

    void TeclaPressionada(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Esconder();
            e.Handled = true;
        }
    }

    public void Mostrar()
    {
        AjustarJanela(_config.Atual.Janela);
        AppWindow.Show();
        Activate();
        SetForegroundWindow(_hwnd);
    }

    void Esconder() => AppWindow.Hide();

    void Alternar()
    {
        if (AppWindow.IsVisible && GetForegroundWindow() == _hwnd) Esconder();
        else Mostrar();
    }

    void Sair()
    {        _saindo = true;
        Close();
    }

    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
}
