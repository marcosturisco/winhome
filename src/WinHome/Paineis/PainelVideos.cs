using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using LibVLCSharp.Platforms.Windows;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinHome.Configuracao;
using WinHome.Servicos;
using MediaPlayerVlc = LibVLCSharp.Shared.MediaPlayer;

namespace WinHome.Paineis;

// Vídeos: arquivos e links tocam no VLC instalado, dentro do painel (LibVLCSharp); links do YouTube tocam no
// player do YouTube (WebView2). A Home é remontada a cada mudança de config e a superfície de vídeo não sobrevive
// a isso; por isso o que está tocando fica na Sessao e o painel novo continua do mesmo ponto.
public static class PainelVideos
{
    static class Sessao
    {
        public static string? Fonte;          // caminho, link ou link do YouTube
        public static bool YouTube;
        public static long TempoMs;           // posição atual
        public static bool Tocando;
        public static MediaPlayerVlc? Player; // o do painel visível agora (para pausar ao esconder)
        public static WebView2? Web;
    }

    static bool? _vlcCarregado;
    static string? _erroVlc;
    static Task<CoreWebView2Environment>? _ambienteWeb;

    const string HostLocal = "winhome.local";
    const string TextoInicial = "Abra um arquivo, cole um link ou arraste um vídeo para cá.";
    static readonly string PastaWeb = Path.Combine(AppContext.BaseDirectory, "Assets", "web");
    static readonly string[] TiposMidia =
    [
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".m4v", ".mpg", ".mpeg", ".ts", ".flv", ".3gp",
        ".mp3", ".flac", ".wav", ".ogg", ".m4a", ".aac", ".wma", ".opus", ".m3u", ".m3u8", "*",
    ];

    // O painel visível solta o VLC quando a Home sai da frente (ver HomeMudou no Criar).
    static event Action? SaiuDaFrente;

    // Chamado pela janela ao esconder, minimizar ou (no modo área de trabalho) mandar a Home para trás das janelas.
    public static void HomeEscondida(Videos cfg)
    {
        if (cfg.PausarAoEsconder)
        {
            if (Sessao.Player is { IsPlaying: true } p) p.SetPause(true);
            if (Sessao.Web?.CoreWebView2 is { } web) _ = web.ExecuteScriptAsync("pausar()");
        }
        SaiuDaFrente?.Invoke();
    }

    static string PastaVlc(Videos cfg) => string.IsNullOrWhiteSpace(cfg.PastaVlc)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLAN", "VLC")
        : Environment.ExpandEnvironmentVariables(cfg.PastaVlc);

    // libvlc do VLC instalado, carregada uma vez por execução.
    static bool CarregarVlc(Videos cfg)
    {
        if (_vlcCarregado is { } ok) return ok;
        var pasta = PastaVlc(cfg);
        if (!File.Exists(Path.Combine(pasta, "libvlc.dll")))
        {
            _erroVlc = $"VLC não encontrado em \"{pasta}\". Instale o VLC (64 bits) ou informe a pasta em Configurações → Vídeos.";
            return false; // não guarda: a pasta pode ser corrigida nas Configurações
        }
        try
        {
            Core.Initialize(pasta);
            _vlcCarregado = true;
        }
        catch (Exception ex)
        {
            _erroVlc = $"Não foi possível carregar o VLC: {ex.Message}";
            _vlcCarregado = false;
        }
        return _vlcCarregado.Value;
    }

    // alturaFixa: a linha da Home tem altura definida e a tela fica com o espaço que sobra (em vez de 16:9).
    public static UIElement Criar(Videos cfg, Estilo estilo, Acoes acoes, bool alturaFixa = false)
    {
        var fila = DispatcherQueue.GetForCurrentThread();
        var estado = EstadoVideos.Atual;

        // ---------- Topo: link, arquivo, recentes ----------
        var link = new TextBox { PlaceholderText = "Cole um link (YouTube ou vídeo) e tecle Enter, ou arraste um arquivo aqui", FontFamily = estilo.Fonte };
        var abrirArquivo = new Button { Content = new FontIcon { Glyph = "", FontSize = estilo.Corpo }, Padding = new Thickness(10, 6, 10, 6) };
        ToolTipService.SetToolTip(abrirArquivo, "Abrir arquivo de vídeo ou áudio…");
        var recentes = new Button { Content = new FontIcon { Glyph = "", FontSize = estilo.Corpo }, Padding = new Thickness(10, 6, 10, 6) };
        ToolTipService.SetToolTip(recentes, "Recentes");
        var topo = new Grid { ColumnSpacing = 6 };
        topo.ColumnDefinitions.Add(new ColumnDefinition());
        topo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(abrirArquivo, 1);
        Grid.SetColumn(recentes, 2);
        topo.Children.Add(link);
        topo.Children.Add(abrirArquivo);
        topo.Children.Add(recentes);

        // ---------- Tela (16:9) ----------
        var aviso = new TextBlock
        {
            Text = TextoInicial,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16),
            FontFamily = estilo.Fonte,
        };
        // A superfície do VLC precisa de tamanho real: sem altura, o VLC desistiria dela e abriria uma janela própria.
        var tela = new Grid
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.5)),
            AllowDrop = true,
            MinHeight = alturaFixa ? 60 : 180,
            Height = alturaFixa ? double.NaN : 270,
            Children = { aviso },
        };
        if (!alturaFixa)
            tela.SizeChanged += (_, e) =>
            {
                var altura = Math.Clamp(e.NewSize.Width * 9 / 16, 180, 720); // 16:9 pela largura do painel
                if (double.IsNaN(tela.Height) || Math.Abs(tela.Height - altura) > 1) tela.Height = altura;
            };

        // ---------- Controles (VLC) ----------
        var tocarPausar = BotaoControle("", "Tocar / pausar");
        var parar = BotaoControle("", "Parar");
        var posicao = new Slider { Minimum = 0, Maximum = 1, StepFrequency = 1, IsThumbToolTipEnabled = false, VerticalAlignment = VerticalAlignment.Center };
        var tempo = new TextBlock { Text = "0:00 / 0:00", FontSize = estilo.Legenda, Opacity = 0.8, VerticalAlignment = VerticalAlignment.Center, FontFamily = estilo.Fonte };
        var mudo = BotaoControle("", "Sem som");
        var volume = new Slider { Minimum = 0, Maximum = 100, Width = 90, Value = estado.Volume, VerticalAlignment = VerticalAlignment.Center };
        var externo = BotaoControle("", "Abrir no VLC (tela cheia, do mesmo ponto)");
        var controles = new Grid { ColumnSpacing = 6, Visibility = Visibility.Collapsed };
        UIElement[] colunas = [tocarPausar, parar, posicao, tempo, mudo, volume, externo];
        for (int i = 0; i < colunas.Length; i++)
        {
            controles.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
            Grid.SetColumn((FrameworkElement)colunas[i], i);
            controles.Children.Add(colunas[i]);
        }

        // ---------- Controles (YouTube) ----------
        var pararYouTube = BotaoControle("", "Parar e limpar o painel");
        var noYouTube = new HyperlinkButton { Content = "Abrir no YouTube", Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
        var controlesYouTube = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Visibility = Visibility.Collapsed,
            Children = { pararYouTube, noYouTube },
        };
        ToolTipService.SetToolTip(parar, "Parar e limpar o painel");

        LibVLC? libvlc = null;
        MediaPlayerVlc? player = null;
        VideoView? view = null;
        WebView2? web = null;
        var arrastando = false; // usuário mexendo na barra de posição
        string? pendente = null; // fonte esperando a VideoView ficar pronta

        void Mostrar(string? texto)
        {
            aviso.Text = texto ?? "";
            aviso.Visibility = texto is null ? Visibility.Collapsed : Visibility.Visible;
        }

        void Atualizar(long ms, long total)
        {
            if (!arrastando)
            {
                posicao.Maximum = Math.Max(1, total / 1000.0);
                posicao.Value = Math.Clamp(ms / 1000.0, 0, posicao.Maximum);
            }
            tempo.Text = $"{Formatar(ms)} / {Formatar(total)}";
        }

        // ---------- VLC ----------
        void LiberarVlc()
        {
            var (p, l) = (player, libvlc);
            player = null;
            libvlc = null;
            if (Sessao.Player == p) Sessao.Player = null;
            if (p is null && l is null) return;
            Task.Run(() => { p?.Stop(); p?.Dispose(); l?.Dispose(); }); // Stop pode demorar; fora da interface
        }

        // Parar: encerra o player, tira a imagem do vídeo (VLC ou YouTube) e esquece a sessão — o painel volta ao
        // estado inicial e a Home remontada não retoma esse vídeo.
        void Limpar()
        {
            LiberarVlc();
            pendente = null;
            if (view is not null) { tela.Children.Remove(view); view = null; }
            if (web is not null) { tela.Children.Remove(web); web = null; } // o Unloaded fecha o WebView2
            Sessao.Fonte = null;
            Sessao.TempoMs = 0;
            Sessao.Tocando = false;
            Sessao.YouTube = false;
            link.Text = "";
            controles.Visibility = Visibility.Collapsed;
            controlesYouTube.Visibility = Visibility.Collapsed;
            Glifo(tocarPausar, "");
            Atualizar(0, 0);
            Mostrar(TextoInicial);
        }

        void CriarVideoView()
        {
            view = new VideoView { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            view.Initialized += (_, e) =>
            {
                // Sem a superfície do painel, o VLC abriria uma janela própria: melhor não tocar e avisar.
                if (e.SwapChainOptions is not { Length: > 0 } || tela.ActualHeight < 10)
                {
                    pendente = null;
                    Mostrar("Não foi possível preparar a área de vídeo do painel. Use ⤢ para abrir no VLC.");
                    return;
                }
                libvlc = new LibVLC(["--no-video-title-show", .. e.SwapChainOptions]);
                player = new MediaPlayerVlc(libvlc) { Volume = estado.Volume };
                view.MediaPlayer = player;
                Sessao.Player = player;
                player.TimeChanged += (s, a) =>
                {
                    if (s != player) return; // player já liberado (Stop em segundo plano): o ponto guardado vale
                    Sessao.TempoMs = a.Time;
                    var total = player?.Length ?? 0;
                    fila.TryEnqueue(() => Atualizar(a.Time, total));
                };
                player.Playing += (_, _) => { Sessao.Tocando = true; fila.TryEnqueue(() => { Mostrar(null); Glifo(tocarPausar, ""); }); };
                player.Paused += (_, _) => { Sessao.Tocando = false; fila.TryEnqueue(() => Glifo(tocarPausar, "")); };
                player.EndReached += (_, _) =>
                {
                    Sessao.Tocando = false;
                    Sessao.TempoMs = 0;
                    fila.TryEnqueue(() => { Glifo(tocarPausar, ""); Atualizar(0, 0); Mostrar("Fim. ▶ toca de novo."); });
                };
                player.EncounteredError += (_, _) =>
                {
                    Sessao.Tocando = false;
                    fila.TryEnqueue(() => Mostrar("O VLC não conseguiu abrir este arquivo ou link."));
                };
                if (pendente is { } fonte)
                {
                    pendente = null;
                    TocarVlc(fonte, Sessao.TempoMs);
                }
            };
            view.Unloaded += (_, _) => LiberarVlc(); // a Home foi remontada ou o painel saiu
            tela.Children.Insert(0, view);
        }

        void TocarVlc(string fonte, long inicioMs)
        {
            if (libvlc is null || player is null)
            {
                pendente = fonte;
                if (view is null) CriarVideoView();
                return;
            }
            var media = Uri.TryCreate(fonte, UriKind.Absolute, out var uri) && !uri.IsFile
                ? new Media(libvlc, uri)
                : new Media(libvlc, fonte, FromType.FromPath);
            if (inicioMs > 1000) media.AddOption($":start-time={(inicioMs / 1000.0).ToString(CultureInfo.InvariantCulture)}");
            if (cfg.Repetir) media.AddOption(":input-repeat=65535");
            player.Play(media);
            media.Dispose();
        }

        // ---------- YouTube ----------
        async void TocarYouTube(string id, string? lista, long inicioMs, bool tocar)
        {
            if (web is null)
            {
                web = new WebView2 { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                tela.Children.Insert(0, web);
                Sessao.Web = web;
                try
                {
                    await web.EnsureCoreWebView2Async(await (_ambienteWeb ??= CriarAmbienteWeb()));
                }
                catch (Exception ex)
                {
                    Mostrar($"Não foi possível abrir o player do YouTube: {ex.Message}");
                    return;
                }
                var core = web.CoreWebView2;
                core.SetVirtualHostNameToFolderMapping(HostLocal, PastaWeb, CoreWebView2HostResourceAccessKind.Allow);
                core.Settings.AreDevToolsEnabled = false;
                // Só a página local fica no painel; links clicados dentro do player vão para o navegador.
                core.NavigationStarting += (_, e) =>
                {
                    if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var u) && u.Host != HostLocal)
                    {
                        e.Cancel = true;
                        AbrirNoNavegador(e.Uri);
                    }
                };
                core.NewWindowRequested += (_, e) =>
                {
                    e.Handled = true;
                    AbrirNoNavegador(e.Uri);
                };
                core.WebMessageReceived += (_, e) =>
                {
                    try
                    {
                        using var json = JsonDocument.Parse(e.TryGetWebMessageAsString());
                        Sessao.TempoMs = (long)(json.RootElement.GetProperty("t").GetDouble() * 1000);
                        Sessao.Tocando = json.RootElement.GetProperty("tocando").GetBoolean();
                    }
                    catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { }
                };
                web.Unloaded += (_, _) =>
                {
                    if (Sessao.Web == web) Sessao.Web = null;
                    web.Close();
                };
            }
            var endereco = $"https://{HostLocal}/youtube.html?v={Uri.EscapeDataString(id)}&t={inicioMs / 1000}&auto={(tocar ? 1 : 0)}&vol={estado.Volume}";
            if (lista is not null) endereco += $"&list={Uri.EscapeDataString(lista)}";
            web.Source = new Uri(endereco);
            Mostrar(null);
        }

        // ---------- Abrir uma fonte ----------
        void Abrir(string fonte, long inicioMs = 0, bool tocar = true)
        {
            fonte = fonte.Trim().Trim('"');
            if (fonte.Length == 0) return;
            Sessao.TempoMs = inicioMs;
            Sessao.Fonte = fonte;
            Sessao.Tocando = tocar;
            estado.Lembrar(fonte);
            link.Text = fonte;

            if (YouTubeLink.Ler(fonte) is { } yt)
            {
                Sessao.YouTube = true;
                LiberarVlc();
                if (view is not null) { tela.Children.Remove(view); view = null; }
                controles.Visibility = Visibility.Collapsed;
                controlesYouTube.Visibility = Visibility.Visible;
                TocarYouTube(yt.Id, yt.Lista, inicioMs > 0 ? inicioMs : yt.InicioMs, tocar);
                return;
            }

            Sessao.YouTube = false;
            if (web is not null) { tela.Children.Remove(web); web = null; }
            controlesYouTube.Visibility = Visibility.Collapsed;
            if (!CarregarVlc(cfg))
            {
                Mostrar(_erroVlc);
                return;
            }
            controles.Visibility = Visibility.Visible;
            Mostrar("Carregando…");
            if (tocar) TocarVlc(fonte, inicioMs);
            else
            {
                // O VLC só é criado no ▶: um vídeo pausado não ocupa memória enquanto ninguém assiste.
                pendente = null;
                Mostrar("▶ para continuar de onde parou.");
            }
        }

        link.KeyDown += (_, e) =>
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            Abrir(link.Text);
            e.Handled = true;
        };
        abrirArquivo.Click += async (_, _) =>
        {
            if (await acoes.EscolherArquivo(TiposMidia) is { } arquivo) Abrir(arquivo);
        };
        recentes.Click += (_, _) =>
        {
            var menu = new MenuFlyout();
            foreach (var r in estado.Recentes)
            {
                var item = new MenuFlyoutItem { Text = Rotulo(r) };
                ToolTipService.SetToolTip(item, r);
                item.Click += (_, _) => Abrir(r);
                menu.Items.Add(item);
            }
            if (menu.Items.Count == 0) menu.Items.Add(new MenuFlyoutItem { Text = "Nada ainda", IsEnabled = false });
            else
            {
                menu.Items.Add(new MenuFlyoutSeparator());
                var limpar = new MenuFlyoutItem { Text = "Limpar o histórico", Icon = new FontIcon { Glyph = "" } };
                limpar.Click += (_, _) => ConfirmarLimpezaDoHistorico(recentes, estado);
                menu.Items.Add(limpar);
            }
            menu.ShowAt(recentes);
        };

        tela.DragOver += (_, e) =>
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems) || e.DataView.Contains(StandardDataFormats.WebLink) ||
                e.DataView.Contains(StandardDataFormats.Text))
                e.AcceptedOperation = DataPackageOperation.Link;
        };
        tela.Drop += async (_, e) =>
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems) &&
                (await e.DataView.GetStorageItemsAsync()).OfType<StorageFile>().FirstOrDefault() is { } arquivo)
                Abrir(arquivo.Path);
            else if (e.DataView.Contains(StandardDataFormats.WebLink))
                Abrir((await e.DataView.GetWebLinkAsync()).ToString());
            else if (e.DataView.Contains(StandardDataFormats.Text))
                Abrir(await e.DataView.GetTextAsync());
        };

        tocarPausar.Click += (_, _) =>
        {
            if (player is null)
            {
                if (Sessao.Fonte is { } retomar && !Sessao.YouTube) TocarVlc(retomar, Sessao.TempoMs); // VLC liberado
                return;
            }
            if (player.IsPlaying) player.Pause();
            else if (player.State is VLCState.Paused) player.Play();
            else if (Sessao.Fonte is { } f) TocarVlc(f, Sessao.TempoMs);
        };
        parar.Click += (_, _) => Limpar();
        pararYouTube.Click += (_, _) => Limpar();
        posicao.AddHandler(UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) => arrastando = true), true);
        posicao.AddHandler(UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) =>
        {
            arrastando = false;
            if (player is { IsSeekable: true } p) p.Time = (long)(posicao.Value * 1000);
        }), true);
        posicao.AddHandler(UIElement.PointerCaptureLostEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) => arrastando = false), true);
        mudo.Click += (_, _) =>
        {
            if (player is null) return;
            player.Mute = !player.Mute;
            Glifo(mudo, player.Mute ? "" : "");
        };
        volume.ValueChanged += (_, e) =>
        {
            estado.Volume = (int)e.NewValue;
            if (player is not null) player.Volume = estado.Volume;
        };
        volume.AddHandler(UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) => estado.Gravar()), true);
        externo.Click += (_, _) =>
        {
            if (Sessao.Fonte is not { } f) return;
            var exe = Path.Combine(PastaVlc(cfg), "vlc.exe");
            if (!File.Exists(exe)) { acoes.Resultado($"vlc.exe não encontrado em \"{PastaVlc(cfg)}\"."); return; }
            if (player is { IsPlaying: true } p) p.SetPause(true);
            var info = new ProcessStartInfo(exe) { UseShellExecute = false };
            info.ArgumentList.Add("--fullscreen");
            info.ArgumentList.Add($"--start-time={(Sessao.TempoMs / 1000).ToString(CultureInfo.InvariantCulture)}");
            info.ArgumentList.Add(f);
            Process.Start(info);
        };
        noYouTube.Click += (_, _) =>
        {
            if (Sessao.Fonte is { } f && YouTubeLink.Ler(f) is { } yt)
                AbrirNoNavegador($"https://www.youtube.com/watch?v={yt.Id}&t={Sessao.TempoMs / 1000}s");
        };

        // Home escondida ou atrás das janelas (modo área de trabalho) com o vídeo pausado (ou pausado agora pelo
        // "pausar ao esconder"): solta o VLC — decodificador, quadros e superfície somam centenas de MB num vídeo 4K.
        // O ponto fica na Sessao; o ▶ retoma dali.
        void HomeMudou(bool visivel)
        {
            if (visivel || player is null || (player.IsPlaying && !cfg.PausarAoEsconder)) return;
            Sessao.Tocando = false;
            LiberarVlc();
            if (view is not null) { tela.Children.Remove(view); view = null; }
            Glifo(tocarPausar, "");
            Mostrar("▶ para continuar de onde parou.");
        }
        void Saiu() => HomeMudou(false);
        tela.Loaded += (_, _) => { EstadoHome.Mudou += HomeMudou; SaiuDaFrente += Saiu; };
        tela.Unloaded += (_, _) => { EstadoHome.Mudou -= HomeMudou; SaiuDaFrente -= Saiu; };

        // Painel remontado: continua o que estava aberto, do mesmo ponto.
        if (Sessao.Fonte is { } anterior) Abrir(anterior, Sessao.TempoMs, Sessao.Tocando);

        var rodape = new StackPanel { Children = { controles, controlesYouTube } };
        var painel = new Grid { RowSpacing = 10, Children = { topo, tela, rodape } };
        painel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        painel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // a tela
        painel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(tela, 1);
        Grid.SetRow(rodape, 2);
        return painel;
    }

    // Confirmação rápida junto ao botão de recentes (o vídeo que estiver tocando não é afetado).
    static void ConfirmarLimpezaDoHistorico(Button ancora, EstadoVideos estado)
    {
        var confirmar = new Button
        {
            Content = $"Apagar os {estado.Recentes.Count} itens do histórico",
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
        };
        var flyout = new Flyout
        {
            Content = new StackPanel
            {
                Spacing = 8,
                Children = { new TextBlock { Text = "Limpar a lista de vídeos e links recentes?" }, confirmar },
            },
        };
        confirmar.Click += (_, _) =>
        {
            estado.LimparRecentes();
            flyout.Hide();
        };
        flyout.ShowAt(ancora);
    }

    static async Task<CoreWebView2Environment> CriarAmbienteWeb()
    {
        // Dados do WebView2 (cookies do YouTube etc.) fora da pasta do programa.
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinHome", "WebView2");
        return await CoreWebView2Environment.CreateWithOptionsAsync("", pasta, new CoreWebView2EnvironmentOptions());
    }

    static void AbrirNoNavegador(string endereco)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var u) || (u.Scheme != "https" && u.Scheme != "http")) return;
        try { Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception) { }
    }

    static Button BotaoControle(string glifo, string dica)
    {
        var b = new Button
        {
            Content = new FontIcon { Glyph = glifo, FontSize = 14 },
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(b, dica);
        return b;
    }

    static void Glifo(Button b, string glifo) => ((FontIcon)b.Content).Glyph = glifo;

    static string Formatar(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }

    static string Rotulo(string fonte) =>
        YouTubeLink.Ler(fonte) is not null ? $"YouTube · {fonte}"
        : Uri.TryCreate(fonte, UriKind.Absolute, out var u) && !u.IsFile ? fonte
        : Path.GetFileName(fonte);
}

// Reconhece links do YouTube: watch?v=, youtu.be/, shorts/, live/, embed/ (com t= e list= opcionais).
public sealed record YouTubeLink(string Id, string? Lista, long InicioMs)
{
    static readonly string[] Hosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be", "www.youtube-nocookie.com"];

    public static YouTubeLink? Ler(string texto)
    {
        if (!Uri.TryCreate(texto.Trim(), UriKind.Absolute, out var u) || !Hosts.Contains(u.Host.ToLowerInvariant())) return null;
        var q = System.Web.HttpUtility.ParseQueryString(u.Query);
        var partes = u.AbsolutePath.Trim('/').Split('/');
        string? id = u.Host.EndsWith("youtu.be") ? partes[0]
            : q["v"] is { Length: > 0 } v ? v
            : partes.Length >= 2 && partes[0] is "shorts" or "live" or "embed" ? partes[1]
            : null;
        var lista = q["list"];
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(lista)) return null;
        return new YouTubeLink(id ?? "", lista, Segundos(q["t"] ?? q["start"]) * 1000);
    }

    // "90", "90s", "1m30s", "1h2m3s"
    static long Segundos(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return 0;
        if (long.TryParse(t.TrimEnd('s'), out var s)) return s;
        long total = 0, numero = 0;
        foreach (var c in t)
        {
            if (char.IsDigit(c)) { numero = numero * 10 + (c - '0'); continue; }
            total += c switch { 'h' => numero * 3600, 'm' => numero * 60, 's' => numero, _ => 0 };
            numero = 0;
        }
        return total;
    }
}
