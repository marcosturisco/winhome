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
using Windows.Graphics;
using Windows.System;
using WinHome.Configuracao;
using WinHome.Paineis;
using WinHome.Servicos;

namespace WinHome;

// Janela da Home: sem bordas, ocupa a área de trabalho (ou a tela toda) do monitor onde está e é montada
// a partir da config; o botão ❐ a transforma numa janela comum, para levá-la a outro monitor.
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
    Button? _botaoModo;
    Estilo? _estilo;
    bool _saindo;

    // Modo janela (botão ❐): borda e redimensionável. "janela" da config continua dizendo como ela ocupa a tela.
    // Monitor, modo e retângulo ficam em estado.json (EstadoJanela) para a próxima vez que o WinHome abrir.
    bool _emJanela;
    RectInt32? _retanguloJanela;

    // Modo área de trabalho ("modoAreaDeTrabalho"): a Home fica no fundo, atrás das janelas, como um papel de parede.
    // _naFrente = trazida pelo atalho/bandeja; volta ao fundo quando outro programa é ativado ou com Esc/—/✕.
    bool _naFrente = true;
    readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _gravarEstado;

    // Faixa do topo: arrastar leva a Home a qualquer monitor (em qualquer modo); duplo clique alterna o modo.
    long _ultimoCliqueFaixa;
    Windows.Foundation.Point? _inicioArrasto;
    readonly Border _faixa = new()
    {
        Height = AreaDoTopo,
        VerticalAlignment = VerticalAlignment.Top,
        Background = new SolidColorBrush(Colors.Transparent), // precisa de fundo para receber o clique
        Child = new Border // alça discreta, como em outras janelas arrastáveis
        {
            Width = 48,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Colors.Gray),
            Opacity = 0.35,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };

    // Barra ⚙ — ❐ ✕ compacta: botões de 22 px e a faixa de cima (barra + alça de arrastar) com 26 px; os painéis,
    // as Configurações e os apps ancorados começam logo abaixo dela.
    public const double AlturaBarra = 22;
    public const double AreaDoTopo = AlturaBarra + 4;

    static readonly string ArquivoIcone = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

    public MainWindow()
    {
        Title = "WinHome";
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        AppWindow.SetIcon(ArquivoIcone);

        _config = new ConfigService(DispatcherQueue);
        _editor = new EditorConfig(_config);
        _acoes = new Acoes(AbrirApp, _editor, ResultadoAbertura, EscolherPasta, Informar, EscolherArquivo, InformarComDesfazer);
        _configuracoes = new Telas.PainelConfiguracoes(_editor, _hwnd, AbrirConfig, Sair);
        // Com as Configurações abertas, a Home encolhe para o lado em vez de ficar coberta.
        _configuracoes.AbertoMudou += aberto =>
            _rolagem.Margin = _faixa.Margin = new Thickness(0, 0, aberto ? Telas.PainelConfiguracoes.Largura : 0, 0);

        _busca.TextChanged += (_, _) => RenderizarArea();
        _busca.KeyDown += TeclaNaBusca;
        _rolagem.Content = new StackPanel { Spacing = 24, Children = { _busca, _area } };

        _raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _raiz.RowDefinitions.Add(new RowDefinition());
        var barra = BarraDeBotoes();
        foreach (var elemento in new[] { _rolagem, _configuracoes.Raiz, _faixa, barra }) Grid.SetRow(elemento, 1);
        _raiz.Children.Add(_aviso);
        _raiz.Children.Add(_rolagem);
        _raiz.Children.Add(_configuracoes.Raiz);
        _raiz.Children.Add(_faixa);
        _raiz.Children.Add(barra);
        ToolTipService.SetToolTip(_faixa, "Arraste para levar a Home a outro monitor; duplo clique alterna tela inteira/janela");
        _faixa.PointerPressed += FaixaPressionada;
        _faixa.PointerMoved += FaixaMovida;
        _faixa.PointerReleased += (_, _) => _inicioArrasto = null;
        _faixa.PointerCaptureLost += (_, _) => _inicioArrasto = null;

        // Grava o estado meio segundo depois do último movimento ou redimensionamento.
        _gravarEstado = DispatcherQueue.CreateTimer();
        _gravarEstado.Interval = TimeSpan.FromMilliseconds(500);
        _gravarEstado.IsRepeating = false;
        _gravarEstado.Tick += (_, _) => GravarEstado();
        AppWindow.Changed += (_, e) =>
        {
            if (e.DidPositionChange || e.DidSizeChange) { _gravarEstado.Stop(); _gravarEstado.Start(); }
        };
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
        // Apps ancorados acompanham a área dos painéis e voltam para cima da Home quando ela é ativada.
        Ancoragem.Iniciar(_hwnd, _rolagem, DispatcherQueue);
        Activated += (_, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated) Ancoragem.HomeAtivada();
            // Modo área de trabalho: trazida pelo atalho, volta para o fundo quando outro programa é ativado
            // (um app ancorado conta como parte da Home).
            else if (_config.Atual.ModoAreaDeTrabalho && _naFrente)
                DispatcherQueue.TryEnqueue(() =>
                {
                    var frente = GetForegroundWindow();
                    if (frente != _hwnd && !Ancoragem.EhAncorada(frente)) MandarParaOFundo();
                });
        };

        // Modo área de trabalho: a cada mudança na ordem das janelas, a Home pede para ficar no fundo; se algo a
        // minimizar (Win+D), volta sozinha — ela faz o papel do papel de parede.
        _mensagens.Adicionar((msg, wParam, lParam) =>
        {
            if (!_config.Atual.ModoAreaDeTrabalho) return false;
            if (msg == WM_WINDOWPOSCHANGING && !_naFrente)
            {
                var pos = Marshal.PtrToStructure<PosicaoJanela>(lParam);
                if ((pos.Flags & SWP_NOZORDER) == 0)
                {
                    pos.Depois = HWND_BOTTOM;
                    Marshal.StructureToPtr(pos, lParam, false);
                }
            }
            else if (msg == WM_SIZE && wParam == SIZE_MINIMIZED)
                DispatcherQueue.TryEnqueue(() =>
                {
                    ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
                    MandarParaOFundo();
                });
            return false; // segue o tratamento normal
        });

        Closed += (_, _) =>
        {
            Ancoragem.SoltarTodos(); // os apps voltam ao tamanho e lugar de antes
            if (_gravarEstado.IsRunning) GravarEstado();
            _bandeja.Dispose();
            _atalho.Dispose();
            _mensagens.Dispose();
            _config.Dispose();
        };

        _config.Mudou += Aplicar;
        var erro = _config.Carregar(); // antes de ler Atual
        RestaurarEstado(); // antes de Aplicar: o ajuste da janela usa o monitor onde ela está
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
        // No modo área de trabalho a Home é o "papel de parede": fora da barra de tarefas e do Alt+Tab.
        AppWindow.IsShownInSwitchers = !config.ModoAreaDeTrabalho;

        var erroAtalho = _atalho.Definir(config.Atalho);
        if (erroAtalho is not null) erros.Add(erroAtalho);
        if (Inicializacao.Sincronizar(config.IniciarComWindows) is { } erroInicio) erros.Add(erroInicio);
        AtualizarDicas(config, atalhoValido: erroAtalho is null && !string.IsNullOrWhiteSpace(config.Atalho));

        var margem = Math.Max(24, estilo.Espaco * 3);
        _rolagem.Padding = new Thickness(margem, AreaDoTopo, margem, margem); // painéis logo abaixo da barra de botões
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

    // Chamado a cada mudança de config e ao mostrar: sempre no monitor onde a janela está agora.
    void AjustarJanela(string modo)
    {
        AtualizarBotaoModo();

        if (_emJanela)
        {
            var janela = Sobreposto();
            janela.SetBorderAndTitleBar(true, false);
            janela.IsResizable = true;
            janela.IsMaximizable = false;
            janela.IsMinimizable = true;
            return; // posição e tamanho ficam onde o usuário deixou
        }

        if (modo == "telaCheia")
        {
            if (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen)
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            return;
        }

        var presenter = Sobreposto();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = true;
        // Área de trabalho do monitor onde a janela está: preserva a barra de tarefas.
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.MoveAndResize(area);
    }

    OverlappedPresenter Sobreposto()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter) return presenter;
        AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        return (OverlappedPresenter)AppWindow.Presenter;
    }

    // ❐: tela inteira ↔ janela. Ao virar janela, reusa o último tamanho se ele estava neste monitor;
    // senão, 70% da área de trabalho, centralizada no monitor atual.
    void AlternarModo()
    {
        var atual = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (_emJanela)
        {
            _retanguloJanela = new RectInt32(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);
            _emJanela = false;
            AjustarJanela(_config.Atual.Janela);
            return;
        }

        _emJanela = true;
        AjustarJanela(_config.Atual.Janela);
        var area = atual.WorkArea;
        var retangulo = _retanguloJanela is { } r &&
                        DisplayArea.GetFromRect(r, DisplayAreaFallback.None)?.DisplayId.Value == atual.DisplayId.Value
            ? r
            : new RectInt32(area.X + area.Width * 15 / 100, area.Y + area.Height * 15 / 100, area.Width * 70 / 100, area.Height * 70 / 100);
        AppWindow.MoveAndResize(retangulo);
    }

    void AtualizarBotaoModo()
    {
        if (_botaoModo is null) return;
        ((FontIcon)_botaoModo.Content).Glyph = _emJanela ? "" : "";
        ToolTipService.SetToolTip(_botaoModo, _emJanela
            ? "Ocupar a tela neste monitor"
            : "Modo janela (para mover a Home para outro monitor)");
    }

    // Duplo clique alterna o modo; um clique só prepara o arrasto, que começa quando o mouse anda alguns pixels
    // (assim um clique simples não faz a Home encolher).
    void FaixaPressionada(object sender, PointerRoutedEventArgs e)
    {
        var ponto = e.GetCurrentPoint(_faixa);
        if (!ponto.Properties.IsLeftButtonPressed) return;
        var agora = Environment.TickCount64;
        if (agora - _ultimoCliqueFaixa <= GetDoubleClickTime())
        {
            _ultimoCliqueFaixa = 0;
            _inicioArrasto = null;
            AlternarModo();
            return;
        }
        _ultimoCliqueFaixa = agora;
        _inicioArrasto = ponto.Position;
        _faixa.CapturePointer(e.Pointer);
    }

    void FaixaMovida(object sender, PointerRoutedEventArgs e)
    {
        if (_inicioArrasto is not { } inicio) return;
        var ponto = e.GetCurrentPoint(_faixa).Position;
        if (Math.Abs(ponto.X - inicio.X) < 6 && Math.Abs(ponto.Y - inicio.Y) < 6) return;
        _inicioArrasto = null;
        _faixa.ReleasePointerCaptures();
        Arrastar();
    }

    // Como uma janela maximizada do Windows: ocupando a tela, a Home encolhe sob o cursor e segue o mouse;
    // ao soltar, volta a ocupar a tela no monitor onde caiu. No modo janela, só move.
    void Arrastar()
    {
        var daTelaInteira = !_emJanela;
        if (daTelaInteira && GetCursorPos(out var cursor))
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
            var largura = _retanguloJanela?.Width ?? area.Width * 70 / 100;
            var altura = _retanguloJanela?.Height ?? area.Height * 70 / 100;
            Sobreposto().SetBorderAndTitleBar(true, false);
            AppWindow.MoveAndResize(new RectInt32(cursor.X - largura / 2, cursor.Y - 16, largura, altura));
        }
        ReleaseCapture();
        SendMessage(_hwnd, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero); // laço de mover do Windows; volta ao soltar
        if (daTelaInteira) AjustarJanela(_config.Atual.Janela);
    }

    // Um ponto no centro da janela identifica o monitor; minimizada ou escondida, a posição não vale.
    void GravarEstado()
    {
        _gravarEstado.Stop();
        if (!AppWindow.IsVisible || IsIconic(_hwnd)) return;
        var p = AppWindow.Position;
        var t = AppWindow.Size;
        if (_emJanela) _retanguloJanela = new RectInt32(p.X, p.Y, t.Width, t.Height);
        new EstadoJanela
        {
            CentroX = p.X + t.Width / 2,
            CentroY = p.Y + t.Height / 2,
            EmJanela = _emJanela,
            Janela = _retanguloJanela is { } r ? [r.X, r.Y, r.Width, r.Height] : null,
        }.Gravar();
    }

    // Leva a janela ao monitor salvo (se ainda estiver conectado; senão, o principal) antes do primeiro ajuste.
    void RestaurarEstado()
    {
        if (EstadoJanela.Ler() is not { } estado) return;
        var monitor = DisplayArea.GetFromPoint(new PointInt32(estado.CentroX, estado.CentroY), DisplayAreaFallback.Primary);
        if (estado.Janela is [var x, var y, var largura, var altura] && largura > 100 && altura > 100)
        {
            var r = new RectInt32(x, y, largura, altura);
            if (DisplayArea.GetFromRect(r, DisplayAreaFallback.None)?.DisplayId.Value is { } id && id != 0)
                _retanguloJanela = r; // só se ainda cai num monitor conectado
        }
        if (estado.EmJanela && _retanguloJanela is { } janela)
        {
            _emJanela = true;
            Sobreposto();
            AppWindow.MoveAndResize(janela);
        }
        else
        {
            var area = monitor.WorkArea;
            AppWindow.Move(new PointInt32(area.X, area.Y)); // o ajuste seguinte ocupa este monitor
        }
    }

    FrameworkElement BarraDeBotoes()
    {
        Button Botao(string glifo, string dica, Action acao)
        {
            var b = new Button
            {
                Content = new FontIcon { Glyph = glifo, FontSize = 10 },
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Width = AlturaBarra + 10,
                Height = AlturaBarra,
                MinHeight = 0,
                MinWidth = 0,
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
            Margin = new Thickness(0, 0, 4, 0),
            Spacing = 0,
            Children =
            {
                Botao("", "Configurações", AlternarConfiguracoes),
                Botao("", "Minimizar", Minimizar),
                (_botaoModo = Botao("", "", AlternarModo)),
                Botao("", "Esconder (Esc)", Esconder),
            },
        };
        // Igual a uma janela comum: — minimiza, ❐ alterna tela inteira/janela, ✕ esconde (como Esc e Alt+F4).
        _botaoEsconder = (Button)barra.Children[^1]; // a dica muda com o atalho (AtualizarDicas)
        AtualizarBotaoModo();
        return barra;
    }

    // Dicas que mencionam o atalho e a bandeja acompanham a config.
    void AtualizarDicas(Config config, bool atalhoValido)
    {
        var formas = new List<string>();
        if (atalhoValido) formas.Add(config.Atalho);
        if (config.IconeNaBandeja) formas.Add("o ícone na bandeja");
        var volta = formas.Count > 0 ? $" — volte com {string.Join(" ou ", formas)}" : "";
        if (_botaoEsconder is not null)
            ToolTipService.SetToolTip(_botaoEsconder, config.ModoAreaDeTrabalho
                ? $"Voltar para trás das janelas (Esc){volta}"
                : $"Esconder (Esc){volta}");
        _bandeja.Definir(config.IconeNaBandeja, atalhoValido ? $"{VersaoApp.Texto} — {config.Atalho}" : VersaoApp.Texto);
    }

    void AbrirApp(AppItem item) => ResultadoAbertura(Lancador.Abrir(item));

    void ResultadoAbertura(string? erro)
    {
        if (erro is not null) MostrarAviso([erro]);
        else if (_config.Atual.EsconderAoAbrirApp) Esconder();
    }

    async Task<string?> EscolherPasta()
    {
        var seletor = new Windows.Storage.Pickers.FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, _hwnd);
        seletor.FileTypeFilter.Add("*");
        return (await seletor.PickSingleFolderAsync())?.Path;
    }

    async Task<string?> EscolherArquivo(string[] tipos)
    {
        var seletor = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, _hwnd);
        foreach (var tipo in tipos.Length > 0 ? tipos : ["*"]) seletor.FileTypeFilter.Add(tipo);
        return (await seletor.PickSingleFileAsync())?.Path;
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

    void InformarComDesfazer(string texto, Action desfazer)
    {
        Informar(texto);
        var botao = new Button { Content = "Desfazer" };
        botao.Click += (_, _) =>
        {
            _aviso.IsOpen = false;
            desfazer();
        };
        _aviso.ActionButton = botao;
    }

    void Informar(string texto)
    {
        _aviso.ActionButton = null;
        _aviso.Severity = InfoBarSeverity.Informational;
        _aviso.Title = "";
        _aviso.Message = texto;
        _aviso.IsOpen = true;
    }

    void MostrarAviso(List<string> erros)
    {
        _aviso.ActionButton = null;
        _aviso.Severity = InfoBarSeverity.Warning;
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
        _naFrente = true; // no modo área de trabalho, liberada para vir à frente até outro programa ser ativado
        if (IsIconic(_hwnd)) ShowWindow(_hwnd, SW_RESTORE); // minimizada pelo —
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

    // No modo área de trabalho, "esconder" é voltar para trás das janelas (a Home nunca some).
    void Esconder()
    {
        if (_config.Atual.ModoAreaDeTrabalho) MandarParaOFundo();
        else AppWindow.Hide();
        PainelVideos.HomeEscondida(_config.Atual.Videos);
    }

    // Vai para a barra de tarefas (Esconder tira a Home dela; volta pelo atalho ou pela bandeja).
    void Minimizar()
    {
        if (_config.Atual.ModoAreaDeTrabalho) MandarParaOFundo();
        else ShowWindow(_hwnd, SW_MINIMIZE);
        PainelVideos.HomeEscondida(_config.Atual.Videos);
    }

    void MandarParaOFundo()
    {
        _naFrente = false;
        if (!AppWindow.IsVisible) AppWindow.Show(false);
        SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    void Alternar()
    {
        if (_config.Atual.ModoAreaDeTrabalho)
        {
            if (_naFrente) Esconder(); // volta para trás
            else Mostrar();
            return;
        }
        // Um app ancorado em primeiro plano conta como a Home: o atalho esconde tudo.
        var frente = GetForegroundWindow();
        if (AppWindow.IsVisible && (frente == _hwnd || Ancoragem.EhAncorada(frente))) Esconder();
        else Mostrar();
    }

    // Argumentos do instalador: restaurar um backup e ligar "iniciar com o Windows" na primeira abertura.
    public void AplicarArgumentos(string[] args)
    {
        var i = Array.FindIndex(args, a => a.Equals("--restaurar", StringComparison.OrdinalIgnoreCase));
        if (i >= 0 && i + 1 < args.Length && File.Exists(args[i + 1]))
        {
            var (_, erro) = ServicoBackup.Restaurar(args[i + 1]);
            if (erro is not null) MostrarAviso([erro]);
            else _config.RecarregarAgora();
        }
        if (args.Contains("--iniciar-com-windows", StringComparer.OrdinalIgnoreCase))
            _editor.Alterar(c => c.IniciarComWindows = true);
    }

    public void Sair()
    {
        _saindo = true;
        Close();
    }

    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr depois, int x, int y, int cx, int cy, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    struct PosicaoJanela
    {
        public IntPtr Janela, Depois;
        public int X, Y, Largura, Altura;
        public uint Flags;
    }

    const uint WM_WINDOWPOSCHANGING = 0x0046, WM_SIZE = 0x0005;
    const int SW_SHOWNOACTIVATE = 4;
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
    static readonly IntPtr HWND_BOTTOM = 1, SIZE_MINIMIZED = 1;
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd, int comando);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, uint mensagem, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] static extern bool GetCursorPos(out PontoTela ponto);

    [StructLayout(LayoutKind.Sequential)]
    struct PontoTela { public int X, Y; }

    const int SW_MINIMIZE = 6, SW_RESTORE = 9;
    const uint WM_NCLBUTTONDOWN = 0x00A1;
    static readonly IntPtr HTCAPTION = 2;
}
