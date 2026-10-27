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
    readonly TextBox _busca = new()
    {
        PlaceholderText = "Buscar apps… (comece a digitar)",
        Width = 560,
        HorizontalAlignment = HorizontalAlignment.Left,
    };
    readonly Border _area = new();
    readonly List<Button> _resultados = [];
    readonly ConfigService _config;
    readonly EditorConfig _editor;
    readonly Acoes _acoes;
    readonly Telas.PainelConfiguracoes _configuracoes;
    readonly MensagensJanela _mensagens;
    readonly AtalhoGlobal _atalho;
    readonly IconeBandeja _bandeja;
    Button? _botaoEsconder;
    Estilo? _estilo;
    bool _saindo;

    static readonly string ArquivoIcone = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

    public MainWindow()
    {
        Title = "WinHome";
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        AppWindow.SetIcon(ArquivoIcone);

        _config = new ConfigService(DispatcherQueue);
        _editor = new EditorConfig(_config);
        _acoes = new Acoes(AbrirApp, _editor, ResultadoAbertura);
        _configuracoes = new Telas.PainelConfiguracoes(_editor, _hwnd, AbrirConfig, Sair);
        // Com as Configurações abertas, a Home encolhe para o lado em vez de ficar coberta.
        _configuracoes.AbertoMudou += aberto =>
            _rolagem.Margin = new Thickness(0, 0, aberto ? Telas.PainelConfiguracoes.Largura : 0, 0);

        _busca.TextChanged += (_, _) => RenderizarArea();
        _busca.KeyDown += TeclaNaBusca;
        _rolagem.Content = new StackPanel { Spacing = 24, Children = { _busca, _area } };

        _raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _raiz.RowDefinitions.Add(new RowDefinition());
        var barra = BarraDeBotoes();
        foreach (var elemento in new[] { _rolagem, _configuracoes.Raiz, barra }) Grid.SetRow(elemento, 1);
        _raiz.Children.Add(_aviso);
        _raiz.Children.Add(_rolagem);
        _raiz.Children.Add(_configuracoes.Raiz);
        _raiz.Children.Add(barra);
        _raiz.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(TeclaPressionada), true);
        _raiz.CharacterReceived += LetraDigitada;
        Content = _raiz;

        _mensagens = new MensagensJanela(_hwnd);
        _atalho = new AtalhoGlobal(_mensagens);
        _atalho.Pressionado += Alternar;
        // Os eventos chegam dentro do procedimento da janela; a ação roda logo depois, na fila da interface.
        _bandeja = new IconeBandeja(_mensagens, ArquivoIcone);
        _bandeja.Mostrar += () => DispatcherQueue.TryEnqueue(Mostrar);
        _bandeja.Configuracoes += () => DispatcherQueue.TryEnqueue(() =>
        {
            Mostrar();
            if (!_configuracoes.Aberto) _configuracoes.Abrir();
        });
        _bandeja.Sair += () => DispatcherQueue.TryEnqueue(Sair);

        // Fechar (Alt+F4) só esconde; sair de verdade é pelo menu da bandeja ou pelas Configurações.
        AppWindow.Closing += (_, e) =>
        {
            if (_saindo) return;
            e.Cancel = true;
            Esconder();
        };
        Closed += (_, _) =>
        {
            _bandeja.Dispose();
            _atalho.Dispose();
            _mensagens.Dispose();
            _config.Dispose();
        };

        _config.Mudou += Aplicar;
        var erro = _config.Carregar(); // antes de ler Atual
        Aplicar(_config.Atual, erro, false);
    }

    void Aplicar(Config config, string? erroConfig, bool doArquivo)
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

        var erroAtalho = _atalho.Definir(config.Atalho);
        if (erroAtalho is not null) erros.Add(erroAtalho);
        if (Inicializacao.Sincronizar(config.IniciarComWindows) is { } erroInicio) erros.Add(erroInicio);
        AtualizarDicas(config, atalhoValido: erroAtalho is null && !string.IsNullOrWhiteSpace(config.Atalho));

        var margem = Math.Max(24, estilo.Espaco * 3);
        _rolagem.Padding = new Thickness(margem, margem + 24, margem, margem);
        _busca.Visibility = config.MostrarBusca ? Visibility.Visible : Visibility.Collapsed;
        _busca.FontFamily = estilo.Fonte;
        _busca.CornerRadius = new CornerRadius(Math.Min(estilo.Tema.Cantos, 20));
        _estilo = estilo;
        RenderizarArea();

        _configuracoes.AtualizarTema(escuro);
        if (doArquivo) _configuracoes.Recarregar();
        MostrarAviso(erros);
    }

    // Home normal ou, com texto na busca, os resultados.
    async void RenderizarArea()
    {
        if (_estilo is null) return;
        var termo = _busca.Text.Trim();
        _resultados.Clear();
        if (termo.Length == 0)
        {
            _area.Child = HomeBuilder.Montar(_config.Atual, _estilo, _acoes);
            return;
        }

        var instalados = await AppsInstalados.Obter();
        if (_busca.Text.Trim() != termo) return; // o usuário continuou digitando
        _resultados.Clear();
        _area.Child = HomeBuilder.MontarResultados(_config.Atual, termo, instalados, _estilo, _acoes, _resultados);
    }

    // Enter abre o primeiro resultado; seta para baixo vai para os resultados.
    void TeclaNaBusca(object sender, KeyRoutedEventArgs e)
    {
        if (_resultados.Count == 0) return;
        if (e.Key == VirtualKey.Enter && _resultados[0].Tag is AppItem primeiro)
        {
            AbrirApp(primeiro);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Down)
        {
            _resultados[0].Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    // Digitar com o foco fora de uma caixa de texto começa uma busca.
    void LetraDigitada(UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
        if (!_config.Atual.MostrarBusca || char.IsControl(e.Character)) return;
        if (FocusManager.GetFocusedElement(_raiz.XamlRoot) is TextBox or AutoSuggestBox or NumberBox or ComboBox) return;
        if (_configuracoes.Aberto) return;
        _busca.Focus(FocusState.Keyboard);
        _busca.Text += e.Character;
        _busca.SelectionStart = _busca.Text.Length;
        e.Handled = true;
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

        var barra = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12),
            Spacing = 4,
            Children =
            {
                Botao("", "Configurações", AlternarConfiguracoes),
                Botao("", "Esconder (Esc)", Esconder),
            },
        };
        _botaoEsconder = (Button)barra.Children[^1]; // a dica muda com o atalho (AtualizarDicas)
        return barra;
    }

    // Dicas que mencionam o atalho e a bandeja acompanham a config.
    void AtualizarDicas(Config config, bool atalhoValido)
    {
        var formas = new List<string>();
        if (atalhoValido) formas.Add(config.Atalho);
        if (config.IconeNaBandeja) formas.Add("o ícone na bandeja");
        var volta = formas.Count > 0 ? $" — volte com {string.Join(" ou ", formas)}" : "";
        if (_botaoEsconder is not null) ToolTipService.SetToolTip(_botaoEsconder, $"Esconder (Esc){volta}");
        _bandeja.Definir(config.IconeNaBandeja, atalhoValido ? $"WinHome — {config.Atalho}" : "WinHome");
    }

    void AbrirApp(AppItem item) => ResultadoAbertura(Lancador.Abrir(item));

    void ResultadoAbertura(string? erro)
    {
        if (erro is not null) MostrarAviso([erro]);
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

    void AlternarConfiguracoes()
    {
        if (_configuracoes.Aberto) _configuracoes.Fechar();
        else _configuracoes.Abrir();
    }

    // Esc fecha uma coisa por vez: Configurações, depois a busca, depois a Home.
    void TeclaPressionada(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape || e.Handled) return;
        if (_configuracoes.Aberto) _configuracoes.Fechar();
        else if (_busca.Text.Length > 0) _busca.Text = "";
        else Esconder();
        e.Handled = true;
    }

    public void Mostrar()
    {
        AjustarJanela(_config.Atual.Janela);
        AppWindow.Show();
        Activate();
        SetForegroundWindow(_hwnd);
        if (_config.Atual.MostrarBusca && !_configuracoes.Aberto)
        {
            _busca.Text = "";
            _busca.Focus(FocusState.Programmatic);
        }
    }

    void Esconder() => AppWindow.Hide();

    void Alternar()
    {
        if (AppWindow.IsVisible && GetForegroundWindow() == _hwnd) Esconder();
        else Mostrar();
    }

    void Sair()
    {
        _saindo = true;
        Close();
    }

    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
}
