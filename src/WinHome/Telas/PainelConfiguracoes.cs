using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using WinHome.Configuracao;
using WinHome.Paineis;
using WinHome.Servicos;

namespace WinHome.Telas;

// Painel lateral de Configurações. Cada mudança é aplicada na hora (a Home atrás atualiza ao vivo)
// e gravada no config.json.
public sealed class PainelConfiguracoes
{
    static readonly string[] Fontes =
        ["Segoe UI Variable", "Segoe UI", "Bahnschrift", "Cascadia Code", "Calibri", "Consolas", "Arial", "Georgia"];
    static readonly string[] CoresProntas =
        ["#4F8CFF", "#7C5CFF", "#E5487E", "#FF6B3D", "#F5B83D", "#3DBE8B", "#2EC4D6", "#9AA0A6"];

    readonly EditorConfig _ed;
    readonly IntPtr _hwnd;
    readonly Action _abrirArquivo;
    readonly Action _sair;
    readonly Border _borda;
    readonly StackPanel _secoes = new() { Spacing = 8 };
    readonly Dictionary<string, bool> _expandidas = new() { ["Geral"] = true };
    Expander? _expAparencia, _expPaineis, _expApps, _expMensagens, _expBoasVindas, _expCuriosidades, _expProjetos, _expTarefas, _expAcoes, _expVideos, _expDispositivos, _expAgenda;
    string? _grupoApps;
    List<AppInstalado> _sugestoes = [];

    Config C => _ed.Servico.Atual;

    public const double Largura = 460;

    public FrameworkElement Raiz => _borda;
    public bool Aberto => _borda.Visibility == Visibility.Visible;

    // Disparado ao abrir (true) e fechar (false), para a Home abrir espaço ao lado.
    public event Action<bool>? AbertoMudou;

    public PainelConfiguracoes(EditorConfig editor, IntPtr hwnd, Action abrirArquivo, Action sair)
    {
        _ed = editor;
        _hwnd = hwnd;
        _abrirArquivo = abrirArquivo;
        _sair = sair;
        _ed.EstruturaMudou += () =>
        {
            if (!Aberto) return;
            if (_expPaineis is not null) _expPaineis.Content = ConteudoPaineis();
            if (_expAparencia is not null) _expAparencia.Content = ConteudoAparencia(); // tema aplicado: controles e galeria
            if (_expApps is not null) _expApps.Content = ConteudoApps();
            if (_expMensagens is not null) _expMensagens.Content = ConteudoMensagens();
            if (_expBoasVindas is not null) _expBoasVindas.Content = ConteudoBoasVindas();
            if (_expCuriosidades is not null) _expCuriosidades.Content = ConteudoCuriosidades();
            if (_expProjetos is not null) _expProjetos.Content = ConteudoProjetos();
            if (_expTarefas is not null) _expTarefas.Content = ConteudoTarefas();
            if (_expAcoes is not null) _expAcoes.Content = ConteudoAcoes();
            if (_expVideos is not null) _expVideos.Content = ConteudoVideos();
            if (_expDispositivos is not null) _expDispositivos.Content = ConteudoDispositivos();
            if (_expAgenda is not null) _expAgenda.Content = ConteudoAgenda();
        };

        var cabecalho = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = "Configurações", FontSize = 24, FontWeight = FontWeights.SemiBold },
                new TextBlock
                {
                    Text = "As mudanças valem na hora e ficam salvas no config.json.",
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };
        // Versão em uso, com link para as novidades dela no GitHub.
        var novidades = new HyperlinkButton { Content = "Novidades desta versão", Padding = new Thickness(0), FontSize = 12 };
        novidades.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(VersaoApp.PaginaDoRelease) { UseShellExecute = true }); }
            catch (Exception) { }
        };
        cabecalho.Children.Insert(1, new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = VersaoApp.Texto, FontSize = 12, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center },
                novidades,
            },
        });
        var arquivo = new HyperlinkButton { Content = "Editar o config.json à mão", Padding = new Thickness(0) };
        arquivo.Click += (_, _) => _abrirArquivo();
        cabecalho.Children.Add(arquivo);

        var conteudo = new StackPanel { Spacing = 16, Children = { cabecalho, _secoes } };
        _borda = new Border
        {
            Width = Largura,
            HorizontalAlignment = HorizontalAlignment.Right,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Visibility = Visibility.Collapsed,
            Child = new ScrollViewer
            {
                Padding = new Thickness(24, 64, 24, 24),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = conteudo,
            },
        };
    }

    public void Abrir()
    {
        Montar();
        _borda.Visibility = Visibility.Visible;
        AbertoMudou?.Invoke(true);
    }

    public void Fechar()
    {
        _borda.Visibility = Visibility.Collapsed;
        AbertoMudou?.Invoke(false);
    }

    // Config relida do arquivo (edição externa): os controles precisam refletir os valores novos.
    public void Recarregar()
    {
        if (Aberto) Montar();
    }

    public void AtualizarTema(bool escuro)
    {
        _borda.Background = new SolidColorBrush(escuro ? Color.FromArgb(0xF5, 0x1E, 0x1F, 0x24) : Color.FromArgb(0xF5, 0xF6, 0xF6, 0xF8));
        _borda.BorderBrush = new SolidColorBrush(escuro ? Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x20, 0, 0, 0));
    }

    void Montar()
    {
        _secoes.Children.Clear();
        _secoes.Children.Add(Secao("Geral", ConteudoGeral()));
        _secoes.Children.Add(_expAparencia = Secao("Aparência", ConteudoAparencia()));
        _secoes.Children.Add(_expBoasVindas = Secao("Boas-vindas", ConteudoBoasVindas()));
        _secoes.Children.Add(_expMensagens = Secao("Mensagens", ConteudoMensagens()));
        _secoes.Children.Add(_expCuriosidades = Secao("Curiosidades", ConteudoCuriosidades()));
        _secoes.Children.Add(_expProjetos = Secao("Projetos", ConteudoProjetos()));
        _secoes.Children.Add(_expTarefas = Secao("Tarefas", ConteudoTarefas()));
        _secoes.Children.Add(_expAcoes = Secao("Ações rápidas", ConteudoAcoes()));
        _secoes.Children.Add(_expVideos = Secao("Vídeos", ConteudoVideos()));
        _secoes.Children.Add(_expDispositivos = Secao("Dispositivos", ConteudoDispositivos()));
        _secoes.Children.Add(_expAgenda = Secao("Agenda", ConteudoAgenda()));
        _secoes.Children.Add(Secao("Backup", ConteudoBackup()));
        _secoes.Children.Add(_expPaineis = Secao("Painéis", ConteudoPaineis()));
        _secoes.Children.Add(_expApps = Secao("Apps", ConteudoApps()));
    }

    // ---------- Seções ----------

    UIElement ConteudoGeral()
    {
        var sair = new Button { Content = "Sair do WinHome" };
        ToolTipService.SetToolTip(sair, "Fecha a Home de vez (o atalho e o ícone da bandeja param até abrir de novo)");
        sair.Click += (_, _) => _sair();

        return Pilha(
        Texto("Nome na saudação", C.Nome, v => _ed.Alterar(c => c.Nome = v)),
        Escolha("Janela", [("Maximizada (mantém a barra de tarefas)", "maximizada"), ("Tela cheia", "telaCheia")],
            C.Janela, v => _ed.Alterar(c => c.Janela = v)),
        Texto("Tecla de atalho (Enter para aplicar)", C.Atalho, v => _ed.Alterar(c => c.Atalho = v),
            aoConfirmar: true, exemplo: "Ctrl+Alt+Space"),
        Alternador("Iniciar com o Windows", C.IniciarComWindows, v => _ed.Alterar(c => c.IniciarComWindows = v)),
        Alternador("Esconder a Home ao abrir um app", C.EsconderAoAbrirApp, v => _ed.Alterar(c => c.EsconderAoAbrirApp = v)),
        Alternador("Mostrar a busca", C.MostrarBusca, v => _ed.Alterar(c => c.MostrarBusca = v)),
        Alternador("Ícone na bandeja do sistema", C.IconeNaBandeja, v => _ed.Alterar(c => c.IconeNaBandeja = v)),
        Alternador("Modo área de trabalho (a Home fica atrás das janelas)", C.ModoAreaDeTrabalho,
            v => _ed.Alterar(c => c.ModoAreaDeTrabalho = v)),
        new TextBlock
        {
            Text = "Ligado: a Home sai da barra de tarefas e do Alt+Tab e fica no fundo, como um papel de parede. " +
                   "O atalho ou a bandeja a trazem para a frente; clicar em outro programa, Esc, — ou ✕ a mandam de volta.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.6,
            FontSize = 12,
        },
        sair);
    }

    UIElement ConteudoAparencia()
    {
        var t = C.Tema;

        var corFundo = BotaoCor("Cor do fundo", t.CorFundo, v => _ed.Alterar(c => c.Tema.CorFundo = v), prontas: false);
        var imagem = SeletorImagem(t.ImagemFundo);
        void Visibilidades(string fundo)
        {
            corFundo.Visibility = fundo == "cor" ? Visibility.Visible : Visibility.Collapsed;
            imagem.Visibility = fundo == "imagem" ? Visibility.Visible : Visibility.Collapsed;
        }
        Visibilidades(t.Fundo);

        var fonte = new ComboBox
        {
            Header = "Fonte",
            IsEditable = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Fontes,
            Text = t.Fonte,
        };
        fonte.SelectionChanged += (_, _) =>
        {
            if (fonte.SelectedItem is string f) _ed.Alterar(c => c.Tema.Fonte = f);
        };
        fonte.TextSubmitted += (_, e) => _ed.Alterar(c => c.Tema.Fonte = e.Text);

        return Pilha(
            GaleriaDeTemas(),
            Subtitulo("Ajuste fino"),
            Escolha("Tema", [("Escuro", "escuro"), ("Claro", "claro"), ("Igual ao Windows", "sistema")],
                t.Modo, v => _ed.Alterar(c => c.Tema.Modo = v)),
            BotaoCor("Cor de destaque", t.CorDestaque, v => _ed.Alterar(c => c.Tema.CorDestaque = v), prontas: true),
            Escolha("Fundo",
                [("Mica (translúcido)", "mica"), ("Mica alternativo", "micaAlt"), ("Acrílico", "acrilico"),
                 ("Cor sólida", "cor"), ("Imagem", "imagem")],
                t.Fundo, v => { Visibilidades(v); _ed.Alterar(c => c.Tema.Fundo = v); }),
            corFundo,
            imagem,
            fonte,
            Deslizante("Opacidade dos painéis (%)", 0, 100, 5, t.OpacidadePaineis * 100,
                v => _ed.Alterar(c => c.Tema.OpacidadePaineis = v / 100)),
            Deslizante("Tamanho dos ícones", 24, 128, 4, t.TamanhoIcone, v => _ed.Alterar(c => c.Tema.TamanhoIcone = v)),
            Deslizante("Cantos arredondados", 0, 32, 1, t.Cantos, v => _ed.Alterar(c => c.Tema.Cantos = v)),
            Deslizante("Espaçamento", 0, 48, 2, t.Espacamento, v => _ed.Alterar(c => c.Tema.Espacamento = v)),
            Deslizante("Colunas da grade", 1, 12, 1, t.Colunas, v => _ed.Alterar(c => c.Tema.Colunas = (int)v)));
    }

    UIElement ConteudoBoasVindas()
    {
        var bv = C.BoasVindas;
        var pilha = Pilha(
            Alternador("Mostrar o painel de boas-vindas na Home", _ed.Exibido(Config.PainelBoasVindas),
                v => _ed.Exibir(Config.PainelBoasVindas, v)),
            Alternador("Mostrar a saudação", bv.MostrarSaudacao, v => _ed.Alterar(c => c.BoasVindas.MostrarSaudacao = v)),
            Alternador("Mostrar a data", bv.MostrarData, v => _ed.Alterar(c => c.BoasVindas.MostrarData = v)),
            Alternador("Mostrar os segundos", bv.MostrarSegundos, v => _ed.Alterar(c => c.BoasVindas.MostrarSegundos = v)),

            Subtitulo("Clima"),
            Alternador("Mostrar o clima", bv.Clima.Visivel, v => _ed.Alterar(c => c.BoasVindas.Clima.Visivel = v)),
            Escolha("Unidade", [("Celsius (°C)", "C"), ("Fahrenheit (°F)", "F")], bv.Clima.Unidade,
                v => _ed.Alterar(c => c.BoasVindas.Clima.Unidade = v)));

        var cidades = bv.Clima.Cidades;
        for (int i = 0; i < cidades.Count; i++)
        {
            var cidade = cidades[i];
            var nome = new TextBlock
            {
                Text = Servicos.ServicoClima.Descrever(cidade),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            pilha.Children.Add(Linha(new FontIcon { Glyph = "", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, nome, null,
                Icone("", "Subir", i > 0, () => _ed.MoverCidade(cidade, -1)),
                Icone("", "Descer", i < cidades.Count - 1, () => _ed.MoverCidade(cidade, +1)),
                Icone("", "Remover cidade", true, () => _ed.RemoverCidade(cidade))));
        }
        pilha.Children.Add(BuscaDeCidades());

        pilha.Children.Add(Subtitulo("Cotações (em reais)"));
        pilha.Children.Add(Alternador("Mostrar as cotações", bv.Cotacoes.Visivel, v => _ed.Alterar(c => c.BoasVindas.Cotacoes.Visivel = v)));
        var moedas = bv.Cotacoes.Moedas;
        for (int i = 0; i < moedas.Count; i++)
        {
            var codigo = moedas[i];
            var nome = new TextBlock
            {
                Text = $"{codigo} — {Servicos.ServicoCotacoes.Nome(codigo)}",
                VerticalAlignment = VerticalAlignment.Center,
            };
            pilha.Children.Add(Linha(new FontIcon { Glyph = "", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, nome, null,
                Icone("", "Subir", i > 0, () => _ed.MoverMoeda(codigo, -1)),
                Icone("", "Descer", i < moedas.Count - 1, () => _ed.MoverMoeda(codigo, +1)),
                Icone("", "Remover moeda", true, () => _ed.RemoverMoeda(codigo))));
        }

        var disponiveis = Servicos.ServicoCotacoes.Conhecidas.Where(m => !moedas.Contains(m.Codigo)).ToList();
        if (disponiveis.Count > 0)
        {
            var menu = new MenuFlyout();
            foreach (var (cod, nomeMoeda) in disponiveis)
            {
                var item = new MenuFlyoutItem { Text = $"{cod} — {nomeMoeda}" };
                item.Click += (_, _) => _ed.AdicionarMoeda(cod);
                menu.Items.Add(item);
            }
            pilha.Children.Add(new DropDownButton { Content = "+ Adicionar moeda", Flyout = menu });
        }

        pilha.Children.Add(new TextBlock
        {
            Text = "Dados da internet: clima pelo Open-Meteo (atualiza a cada 30 min) e cotações pela AwesomeAPI (a cada 15 min). Sem conexão, aparece o último valor salvo.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.6,
            FontSize = 12,
        });
        return pilha;
    }

    // Caixa que busca cidades no Open-Meteo enquanto digita (espera uma pausa na digitação).
    UIElement BuscaDeCidades()
    {
        var caixa = new AutoSuggestBox
        {
            Header = "Adicionar cidade",
            PlaceholderText = "Digite o nome da cidade…",
            QueryIcon = new SymbolIcon(Symbol.Add),
        };
        var achadas = new List<Cidade>();
        var espera = _borda.DispatcherQueue.CreateTimer();
        espera.Interval = TimeSpan.FromMilliseconds(400);
        espera.IsRepeating = false;
        espera.Tick += async (_, _) =>
        {
            var termo = caixa.Text.Trim();
            if (termo.Length < 2) return;
            var lista = await Servicos.ServicoClima.BuscarCidades(termo);
            if (caixa.Text.Trim() != termo) return; // continuou digitando
            achadas = lista;
            caixa.ItemsSource = lista.Count > 0
                ? lista.Select(Servicos.ServicoClima.Descrever).ToList()
                : ["Nenhuma cidade encontrada (ou sem conexão)"];
        };
        caixa.TextChanged += (s, e) =>
        {
            if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            espera.Stop();
            espera.Start();
        };
        caixa.QuerySubmitted += (s, e) =>
        {
            var escolhida = e.ChosenSuggestion is string texto
                ? achadas.FirstOrDefault(c => Servicos.ServicoClima.Descrever(c) == texto)
                : achadas.FirstOrDefault();
            if (escolhida is null) return;
            s.Text = "";
            s.ItemsSource = null;
            _ed.AdicionarCidade(escolhida);
        };
        return caixa;
    }

    static TextBlock Subtitulo(string texto) => new()
    {
        Text = texto,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 0),
    };

    UIElement ConteudoMensagens()
    {
        var pilha = Pilha(
            Alternador("Mostrar a mensagem do dia na Home", _ed.Exibido(Config.PainelMensagem),
                v => _ed.Exibir(Config.PainelMensagem, v)),
            Escolha("Frases exibidas",
                [("Minhas e as do WinHome", "ambas"), ("Só as do WinHome", "embutidas"), ("Só as minhas", "minhas")],
                C.Mensagens.Fonte, v => _ed.Alterar(c => c.Mensagens.Fonte = v)),
            new TextBlock { Text = "Minhas frases", Margin = new Thickness(0, 4, 0, 0) });

        foreach (var frase in C.Mensagens.Minhas)
        {
            var texto = new TextBox { Text = frase.Texto, PlaceholderText = "Frase", TextWrapping = TextWrapping.Wrap };
            texto.TextChanged += (_, _) => _ed.Alterar(c => frase.Texto = texto.Text);
            var autor = new TextBox { Text = frase.Autor, PlaceholderText = "Autor", Width = 130, VerticalAlignment = VerticalAlignment.Top };
            autor.TextChanged += (_, _) => _ed.Alterar(c => frase.Autor = autor.Text);
            var linha = Linha(new Border(), texto, autor, Icone("", "Remover frase", true, () => _ed.RemoverFrase(frase)));
            pilha.Children.Add(linha);
        }

        var nova = new Button { Content = "+ Nova frase" };
        nova.Click += (_, _) => _ed.AdicionarFrase();
        pilha.Children.Add(nova);
        return pilha;
    }

    UIElement ConteudoCuriosidades() => Pilha(
        Alternador("Mostrar as curiosidades do dia na Home", _ed.Exibido(Config.PainelCuriosidade),
            v => _ed.Exibir(Config.PainelCuriosidade, v)),
        Alternador("Incluir nascimentos", C.Curiosidades.IncluirNascimentos, v => _ed.Alterar(c => c.Curiosidades.IncluirNascimentos = v)),
        Alternador("Incluir mortes", C.Curiosidades.IncluirMortes, v => _ed.Alterar(c => c.Curiosidades.IncluirMortes = v)),
        new TextBlock
        {
            Text = "Fatos da data de hoje, com foto, da Wikipédia em português (\"Neste dia\"). Primeiro os destacados pela Wikipédia, depois os demais eventos. Uma busca por dia; sem conexão, aparece o texto salvo.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.6,
            FontSize = 12,
        });

    UIElement ConteudoProjetos()
    {
        var pj = C.Projetos;
        var pilha = Pilha(
            Alternador("Mostrar os projetos recentes na Home", _ed.Exibido(Config.PainelProjetos),
                v => _ed.Exibir(Config.PainelProjetos, v)),
            Escolha("Clique no projeto abre no",
                [("Editor onde foi aberto por último", "origem"), ("VS Code", "vscode"), ("IntelliJ IDEA", "intellij"), ("Android Studio", "androidStudio")],
                pj.EditorPadrao, v => _ed.Alterar(c => c.Projetos.EditorPadrao = v)),
            Deslizante("Quantidade máxima na lista", 3, 20, 1, pj.Maximo, v => _ed.Alterar(c => c.Projetos.Maximo = (int)v)),

            Subtitulo("Fontes"),
            Alternador("Recentes do IntelliJ IDEA", pj.UsarIntellij, v => _ed.Alterar(c => c.Projetos.UsarIntellij = v)),
            Alternador("Recentes do Android Studio", pj.UsarAndroidStudio, v => _ed.Alterar(c => c.Projetos.UsarAndroidStudio = v)),
            Alternador("Recentes do VS Code", pj.UsarVsCode, v => _ed.Alterar(c => c.Projetos.UsarVsCode = v)),
            new TextBlock { Text = "Pastas com projetos (cada subpasta com git vira um projeto)", TextWrapping = TextWrapping.Wrap });

        foreach (var pasta in pj.Pastas)
        {
            var nome = new TextBlock { Text = pasta, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            pilha.Children.Add(Linha(new FontIcon { Glyph = "", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, nome, null,
                Icone("", "Remover pasta", true, () => _ed.RemoverPastaProjetos(pasta))));
        }
        var adicionar = new Button { Content = "+ Adicionar pasta…" };
        adicionar.Click += async (_, _) =>
        {
            var seletor = new FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(seletor, _hwnd);
            seletor.FileTypeFilter.Add("*");
            if (await seletor.PickSingleFolderAsync() is { } pasta) _ed.AdicionarPastaProjetos(pasta.Path);
        };
        pilha.Children.Add(adicionar);

        if (pj.Ocultos.Count > 0)
        {
            pilha.Children.Add(Subtitulo("Ocultos"));
            foreach (var oculto in pj.Ocultos)
            {
                var nome = new TextBlock { Text = oculto, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                pilha.Children.Add(Linha(new FontIcon { Glyph = "", FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, nome, null,
                    Icone("", "Mostrar de novo", true, () => _ed.OcultarProjeto(oculto, false))));
            }
        }

        pilha.Children.Add(Subtitulo("Programas"));
        foreach (var (id, rotulo) in new[] { ("vscode", "VS Code"), ("intellij", "IntelliJ IDEA"), ("androidStudio", "Android Studio") })
        {
            var atual = id switch { "vscode" => pj.Editores.VsCode, "intellij" => pj.Editores.Intellij, _ => pj.Editores.AndroidStudio };
            var detectado = Servicos.ServicoProjetos.Executavel(id, new Editores()) ?? "não encontrado";
            pilha.Children.Add(Texto($"{rotulo} (vazio = detectar; Enter para aplicar)", atual, v => _ed.Alterar(c =>
            {
                if (id == "vscode") c.Projetos.Editores.VsCode = v;
                else if (id == "intellij") c.Projetos.Editores.Intellij = v;
                else c.Projetos.Editores.AndroidStudio = v;
            }), aoConfirmar: true, exemplo: detectado));
        }

        pilha.Children.Add(new TextBlock
        {
            Text = "Só lê o histórico das IDEs e as pastas; nada é alterado. Botão direito num projeto: abrir em outro programa, fixar ou ocultar.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.6,
            FontSize = 12,
        });
        return pilha;
    }

    UIElement ConteudoTarefas()
    {
        var pasta = new HyperlinkButton { Content = "Abrir a pasta do tarefas.json", Padding = new Thickness(0) };
        pasta.Click += (_, _) => _ = Launcher.LaunchFolderPathAsync(ConfigService.Pasta);
        return Pilha(
            Alternador("Mostrar as tarefas na Home", _ed.Exibido(Config.PainelTarefas), v => _ed.Exibir(Config.PainelTarefas, v)),
            Alternador("Mostrar as concluídas (riscadas, no fim da lista)", C.Tarefas.MostrarConcluidas,
                v => _ed.Alterar(c => c.Tarefas.MostrarConcluidas = v)),
            new TextBlock
            {
                Text = "Na Home: Enter adiciona, marcar conclui, ✕ remove; duplo clique ou botão direito edita e move. " +
                       "A lista fica em tarefas.json, separada da config, e não sai do PC.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
            },
            pasta);
    }

    UIElement ConteudoAcoes()
    {
        var pilha = Pilha(
            Alternador("Mostrar as ações rápidas na Home", _ed.Exibido(Config.PainelAcoes), v => _ed.Exibir(Config.PainelAcoes, v)),
            new TextBlock
            {
                Text = "Cada ação executa os passos em ordem e sempre pede confirmação antes, mostrando o comando exato. " +
                       $"Nos comandos, {Servicos.ServicoAcoes.MarcaPasta} vira cada pasta de Projetos.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
            });

        var lista = C.AcoesRapidas;
        for (int i = 0; i < lista.Count; i++)
        {
            var acao = lista[i];
            var icone = new TextBox { Text = acao.Icone, Width = 56, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            ToolTipService.SetToolTip(icone, "Ícone (um emoji; Win+. abre o seletor de emojis)");
            icone.TextChanged += (_, _) => _ed.Alterar(c => acao.Icone = icone.Text);
            var nome = new TextBox { Text = acao.Nome, PlaceholderText = "Nome da ação", VerticalAlignment = VerticalAlignment.Center };
            nome.TextChanged += (_, _) => _ed.Alterar(c => acao.Nome = nome.Text);
            var descricao = new TextBox
            {
                Text = acao.Descricao,
                PlaceholderText = "Para que serve (aparece na dica e na confirmação)",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
            };
            descricao.TextChanged += (_, _) => _ed.Alterar(c => acao.Descricao = descricao.Text);

            var bloco = Pilha(Linha(icone, nome, null,
                Icone("\uE70E", "Subir", i > 0, () => _ed.MoverAcao(acao, -1)),
                Icone("\uE70D", "Descer", i < lista.Count - 1, () => _ed.MoverAcao(acao, +1)),
                Icone("\uE74D", "Remover ação", true, () => _ed.RemoverAcao(acao))),
                descricao);
            bloco.Spacing = 6;
            if (Servicos.ServicoAcoes.PedeArquivo(acao))
            {
                var tipo = new TextBox { Header = "Tipo do arquivo pedido ({arquivo})", Text = acao.TipoArquivo, PlaceholderText = ".apk  ou  *" };
                tipo.TextChanged += (_, _) => _ed.Alterar(c => acao.TipoArquivo = string.IsNullOrWhiteSpace(tipo.Text) ? "*" : tipo.Text.Trim());
                bloco.Children.Add(tipo);
            }

            var passos = new StackPanel { Spacing = 4, Margin = new Thickness(24, 0, 0, 0) };
            for (int j = 0; j < acao.Passos.Count; j++)
            {
                var passo = acao.Passos[j];
                var textos = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = string.IsNullOrWhiteSpace(passo.Nome) ? "(sem nome)" : passo.Nome, TextTrimming = TextTrimming.CharacterEllipsis },
                        new TextBlock
                        {
                            Text = Servicos.ServicoAcoes.Descrever(passo),
                            FontSize = 11,
                            Opacity = 0.6,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                        },
                    },
                };
                ToolTipService.SetToolTip(textos, Servicos.ServicoAcoes.Descrever(passo));
                var glifo = passo.App is not null || passo.Editor is not null ? "\uE7C4" : "\uE756";
                passos.Children.Add(Linha(new FontIcon { Glyph = glifo, FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, textos, null,
                    Icone("\uE70E", "Subir", j > 0, () => _ed.MoverPasso(acao, passo, -1)),
                    Icone("\uE70D", "Descer", j < acao.Passos.Count - 1, () => _ed.MoverPasso(acao, passo, +1)),
                    Icone("\uE711", "Remover passo", true, () => _ed.RemoverPasso(acao, passo))));
            }
            passos.Children.Add(BuscaDeInstalados("Adicionar app", app =>
                _ed.AdicionarPasso(acao, new PassoAcao { Nome = app.Nome, App = app.App, Caminho = app.App is null ? app.Caminho : null })));
            passos.Children.Add(BotaoNovoComando(acao));

            bloco.Children.Add(passos);
            bloco.Margin = new Thickness(0, 8, 0, 0);
            pilha.Children.Add(bloco);
        }

        var nova = new Button { Content = "+ Nova ação" };
        nova.Click += (_, _) => _ed.NovaAcao();

        var restaurar = new Button { Content = "Restaurar as ações padrão…" };
        var confirmar = new Button { Content = "Substituir as minhas ações pelas padrão" };
        var flyout = new Flyout { Content = confirmar };
        confirmar.Click += (_, _) =>
        {
            flyout.Hide();
            _ed.RestaurarAcoesPadrao();
        };
        restaurar.Flyout = flyout;

        var botoes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { nova, restaurar } };
        pilha.Children.Add(botoes);

        var faltam = _ed.AcoesPadraoQueFaltam();
        if (faltam.Count > 0)
        {
            var adicionar = new Button { Content = $"+ Adicionar as ações padrão que faltam ({faltam.Count})" };
            ToolTipService.SetToolTip(adicionar, string.Join("\n", faltam));
            adicionar.Click += (_, _) => _ed.AdicionarAcoesQueFaltam();
            pilha.Children.Add(adicionar);
        }
        return pilha;
    }

    // Temas prontos + os salvos pelo usuário; clique aplica (só o visual; colunas e espaçamento ficam).
    UIElement GaleriaDeTemas()
    {
        var grade = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, ItemWidth = 132, ItemHeight = 104 };
        foreach (var t in TemasProntos.Lista) grade.Children.Add(Miniatura(t, remover: null));
        foreach (var t in C.TemasSalvos) grade.Children.Add(Miniatura(t, remover: () => _ed.RemoverTemaSalvo(t)));

        var nome = new TextBox { PlaceholderText = "Nome do tema", Width = 220 };
        var salvar = new Button { Content = "Salvar", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var flyout = new Flyout { Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { nome, salvar } } };
        void Salvar()
        {
            var texto = nome.Text.Trim();
            if (texto.Length == 0) return;
            flyout.Hide();
            nome.Text = "";
            _ed.SalvarTemaAtual(texto);
        }
        salvar.Click += (_, _) => Salvar();
        nome.KeyDown += (_, e) => { if (e.Key == VirtualKey.Enter) Salvar(); };
        var salvarAtual = new Button { Content = "Salvar o visual atual como tema…", Flyout = flyout };

        return Pilha(Subtitulo("Temas prontos"), grade, salvarAtual);
    }

    FrameworkElement Miniatura(TemaSalvo tema, Action? remover)
    {
        var t = tema.Tema;
        var claro = t.Modo == "claro";
        Color Hex(string h, Color padrao) => Estilo.Cor(h, padrao);
        var fundo = t.Fundo switch
        {
            "cor" => Hex(t.CorFundo, Colors.Black),
            "acrilico" => claro ? Color.FromArgb(255, 226, 236, 242) : Color.FromArgb(255, 24, 36, 44),
            _ => claro ? Color.FromArgb(255, 243, 243, 243) : Color.FromArgb(255, 32, 32, 32), // Mica, Mica alt., imagem
        };
        var alfa = (byte)(Math.Clamp(t.OpacidadePaineis, 0, 1) * 255);
        var painel = claro ? Color.FromArgb(alfa, 255, 255, 255) : Color.FromArgb(alfa, 44, 44, 50);
        var cantos = Math.Min(t.Cantos, 12) * 0.6;

        var miniPainel = new Border
        {
            Width = 76,
            Height = 36,
            CornerRadius = new CornerRadius(cantos),
            Background = new SolidColorBrush(painel),
            Padding = new Thickness(8, 6, 8, 6),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new Border { Width = 26, Height = 4, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Hex(t.CorDestaque, Colors.DodgerBlue)), HorizontalAlignment = HorizontalAlignment.Left },
                    new TextBlock { Text = "Aa", FontSize = 12, FontFamily = new FontFamily(t.Fonte), Foreground = new SolidColorBrush(claro ? Colors.Black : Colors.White) },
                },
            },
        };
        var atual = TemasProntos.Igual(t, C.Tema);
        var amostra = new Border
        {
            Width = 116,
            Height = 64,
            CornerRadius = new CornerRadius(Math.Max(2, cantos)),
            Background = new SolidColorBrush(fundo),
            BorderThickness = new Thickness(atual ? 2 : 1),
            BorderBrush = new SolidColorBrush(atual ? Hex(C.Tema.CorDestaque, Colors.DodgerBlue) : Color.FromArgb(60, 128, 128, 128)),
            Child = miniPainel,
        };
        var rotulo = new TextBlock { Text = tema.Nome, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center };
        var botao = new Button
        {
            Content = new StackPanel { Spacing = 4, Children = { amostra, rotulo } },
            Padding = new Thickness(4),
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
        };
        ToolTipService.SetToolTip(botao, remover is null ? $"Aplicar \"{tema.Nome}\"" : $"Aplicar \"{tema.Nome}\" (botão direito: remover)");
        botao.Click += (_, _) => _ed.AplicarTema(t);
        if (remover is not null)
        {
            var menu = new MenuFlyout();
            var item = new MenuFlyoutItem { Text = "Remover este tema", Icon = new FontIcon { Glyph = "" } };
            item.Click += (_, _) => remover();
            menu.Items.Add(item);
            botao.ContextFlyout = menu;
        }
        return botao;
    }

    UIElement ConteudoBackup()
    {
        var resultado = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        var segredos = new CheckBox { Content = "Incluir os endereços secretos da agenda" };
        ToolTipService.SetToolTip(segredos, "Desmarcado: a agenda vai sem o endereço (cole de novo no outro PC). Marcado: guarde o arquivo em lugar seguro.");

        var fazer = new Button { Content = "Fazer backup…" };
        fazer.Click += async (_, _) =>
        {
            var seletor = new FileSavePicker { SuggestedFileName = Servicos.ServicoBackup.NomeSugerido };
            WinRT.Interop.InitializeWithWindow.Initialize(seletor, _hwnd);
            seletor.FileTypeChoices.Add("Backup do WinHome", [".zip"]);
            if (await seletor.PickSaveFileAsync() is not { } arquivo) return;
            var erro = Servicos.ServicoBackup.Fazer(arquivo.Path, segredos.IsChecked == true);
            resultado.Text = erro ?? $"Backup salvo em {arquivo.Path}.";
        };

        var restaurar = new Button { Content = "Restaurar backup…" };
        restaurar.Click += async (_, _) =>
        {
            if (await EscolherArquivo([".zip"]) is not { } origem) return;
            var (conteudo, erro) = Servicos.ServicoBackup.Examinar(origem);
            if (erro is not null) { resultado.Text = erro; return; }
            var dialogo = new ContentDialog
            {
                XamlRoot = _borda.XamlRoot,
                Title = "Restaurar este backup?",
                Content = new TextBlock
                {
                    Text = $"Vai substituir: {string.Join(", ", conteudo!)}.\n\nOs arquivos atuais ficam guardados numa pasta " +
                           "\"antes-da-restauracao-…\" em %APPDATA%\\WinHome. Endereços da agenda que não vierem no backup são " +
                           "mantidos (pelo nome da agenda).",
                    TextWrapping = TextWrapping.Wrap,
                },
                PrimaryButtonText = "Restaurar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = _borda.ActualTheme,
            };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;
            var (reserva, erroRestaurar) = Servicos.ServicoBackup.Restaurar(origem);
            if (erroRestaurar is not null) { resultado.Text = erroRestaurar; return; }
            _ed.Servico.RecarregarAgora(); // a Home e esta tela se atualizam
            resultado.Text = $"Backup restaurado. Os arquivos anteriores ficaram em {reserva}.";
        };

        return Pilha(
            new TextBlock
            {
                Text = "Um arquivo .zip com a configuração, as tarefas e os vídeos recentes — para guardar ou levar a Home " +
                       "para outro PC. A posição da janela não vai (é deste PC).",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontSize = 12,
            },
            segredos,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { fazer, restaurar } },
            resultado);
    }

    UIElement ConteudoAgenda()
    {
        var cfg = C.Agenda;
        var pilha = Pilha(
            Alternador("Mostrar a agenda na Home", _ed.Exibido(Config.PainelAgenda), on => _ed.Exibir(Config.PainelAgenda, on)),
            Deslizante("Dias mostrados (1 = só hoje)", 1, 7, 1, cfg.Dias, v => _ed.Alterar(c => c.Agenda.Dias = (int)v)),
            new TextBlock
            {
                Text = "No Google Agenda (navegador): ⚙ Configurações → clique na agenda, à esquerda → Integrar agenda → " +
                       "copie o \"Endereço secreto no formato iCal\" e cole abaixo.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.7,
            },
            new InfoBar
            {
                Severity = InfoBarSeverity.Warning,
                IsOpen = true,
                IsClosable = false,
                Title = "Endereço secreto",
                Message = "Quem tiver este endereço consegue ler a sua agenda. Ele fica só no config.json deste PC " +
                          "(nunca no git nem em logs). Não compartilhe; se vazar, use \"Redefinir\" no Google Agenda e cole o novo.",
            });

        foreach (var cal in cfg.Calendarios)
        {
            var nome = new TextBox { Text = cal.Nome, PlaceholderText = "Nome", VerticalAlignment = VerticalAlignment.Center };
            nome.TextChanged += (_, _) => _ed.Alterar(c => cal.Nome = nome.Text);
            var endereco = new PasswordBox
            {
                Password = cal.Endereco,
                PlaceholderText = "https://calendar.google.com/calendar/ical/…/basic.ics",
                PasswordRevealMode = PasswordRevealMode.Peek,
            };
            endereco.PasswordChanged += (_, _) => _ed.Alterar(c => cal.Endereco = endereco.Password.Trim());
            var resultado = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
            var testar = new Button { Content = "Testar" };
            testar.Click += async (_, _) =>
            {
                testar.IsEnabled = false;
                resultado.Text = "Lendo…";
                resultado.Text = await Servicos.ServicoAgenda.Testar(endereco.Password);
                testar.IsEnabled = true;
            };

            var bloco = Pilha(
                Linha(BotaoCor("", cal.Cor, v => _ed.Alterar(c => cal.Cor = v), prontas: false), nome, null,
                    Icone("", "Remover esta agenda", true, () => _ed.RemoverCalendario(cal))),
                endereco,
                Linha(testar, resultado, null));
            bloco.Spacing = 6;
            bloco.Margin = new Thickness(0, 6, 0, 0);
            pilha.Children.Add(bloco);
        }

        var nova = new Button { Content = cfg.Calendarios.Count == 0 ? "+ Adicionar a minha agenda" : "+ Outra agenda" };
        nova.Click += (_, _) => _ed.NovoCalendario();
        pilha.Children.Add(nova);
        return pilha;
    }

    UIElement ConteudoDispositivos()
    {
        var cfg = C.Dispositivos;
        var pilha = Pilha(
            Alternador("Mostrar os dispositivos na Home", _ed.Exibido(Config.PainelDispositivos), on => _ed.Exibir(Config.PainelDispositivos, on)),
            Deslizante("Testar a cada (segundos)", 15, 300, 15, cfg.Intervalo, v => _ed.Alterar(c => c.Dispositivos.Intervalo = (int)v)),
            new TextBlock
            {
                Text = "O teste só abre e fecha uma conexão na porta (até 2 s) e mede a latência; nenhum comando é enviado. " +
                       "Clicar no aparelho na Home mostra as ações do adb para ele, sempre com confirmação.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
            });

        for (int i = 0; i < cfg.Lista.Count; i++)
        {
            var d = cfg.Lista[i];
            var icone = new TextBox { Text = d.Icone, Width = 56, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            ToolTipService.SetToolTip(icone, "Ícone (um emoji; Win+. abre o seletor)");
            icone.TextChanged += (_, _) => _ed.Alterar(c => d.Icone = icone.Text);
            var nome = new TextBox { Text = d.Nome, PlaceholderText = "Nome", VerticalAlignment = VerticalAlignment.Center };
            nome.TextChanged += (_, _) => _ed.Alterar(c => d.Nome = nome.Text);
            var endereco = new TextBox { Text = d.Endereco, PlaceholderText = "IP ou nome na rede (ex.: 192.168.15.11)", VerticalAlignment = VerticalAlignment.Center };
            endereco.TextChanged += (_, _) => _ed.Alterar(c => d.Endereco = endereco.Text.Trim());
            var porta = new NumberBox { Value = d.Porta, Minimum = 1, Maximum = 65535, Width = 110, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden, VerticalAlignment = VerticalAlignment.Center };
            ToolTipService.SetToolTip(porta, "Porta (5555 = adb sem fio)");
            porta.ValueChanged += (_, e) =>
            {
                if (!double.IsNaN(e.NewValue)) _ed.Alterar(c => d.Porta = (int)e.NewValue);
            };

            var bloco = Pilha(
                Linha(icone, nome, null,
                    Icone("", "Subir", i > 0, () => _ed.MoverDispositivo(d, -1)),
                    Icone("", "Descer", i < cfg.Lista.Count - 1, () => _ed.MoverDispositivo(d, +1)),
                    Icone("", "Remover aparelho", true, () => _ed.RemoverDispositivo(d))),
                Linha(new Border { Width = 56 }, endereco, porta));
            bloco.Spacing = 6;
            bloco.Margin = new Thickness(0, 6, 0, 0);
            pilha.Children.Add(bloco);
        }

        var novo = new Button { Content = "+ Novo aparelho" };
        novo.Click += (_, _) => _ed.NovoDispositivo();
        pilha.Children.Add(novo);
        return pilha;
    }

    UIElement ConteudoVideos()
    {
        var v = C.Videos;
        var pastaPadrao = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLAN", "VLC");
        var pastaAtual = string.IsNullOrWhiteSpace(v.PastaVlc) ? pastaPadrao : Environment.ExpandEnvironmentVariables(v.PastaVlc);
        var achou = File.Exists(Path.Combine(pastaAtual, "libvlc.dll"));

        var limpar = new Button { Content = "Limpar a lista de recentes" };
        limpar.Click += (_, _) => Servicos.EstadoVideos.Atual.LimparRecentes();

        return Pilha(
            Alternador("Mostrar o painel de vídeos na Home", _ed.Exibido(Config.PainelVideos), on => _ed.Exibir(Config.PainelVideos, on)),
            Alternador("Repetir o vídeo ao terminar (VLC)", v.Repetir, on => _ed.Alterar(c => c.Videos.Repetir = on)),
            Alternador("Pausar ao esconder ou minimizar a Home", v.PausarAoEsconder, on => _ed.Alterar(c => c.Videos.PausarAoEsconder = on)),
            Texto("Pasta do VLC (vazio = padrão; Enter para aplicar)", v.PastaVlc, t => _ed.Alterar(c => c.Videos.PastaVlc = t.Trim()),
                aoConfirmar: true, exemplo: pastaPadrao),
            new TextBlock
            {
                Text = achou ? $"VLC encontrado em {pastaAtual}." : $"VLC não encontrado em {pastaAtual}: instale o VLC 64 bits (videolan.org).",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = achou ? null : new SolidColorBrush(Colors.OrangeRed),
                Opacity = achou ? 0.6 : 1,
            },
            limpar,
            new TextBlock
            {
                Text = "Arquivos e links (mp4, mkv, mp3, rádios, listas m3u…) tocam no VLC dentro do painel; links do YouTube tocam " +
                       "no player do próprio YouTube. Cole um link, abra um arquivo ou arraste para o painel. O botão ⤢ abre no VLC em tela cheia.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 12,
            });
    }

    // Passo de comando: programa + argumentos; "esperar" roda escondido e mostra a saída na Home.
    Button BotaoNovoComando(AcaoRapida acao)
    {
        var nome = new TextBox { Header = "Nome do passo", PlaceholderText = "Ex.: Conectar ao box" };
        var programa = new TextBox { Header = "Programa, pasta ou documento", PlaceholderText = "Ex.: adb, wt.exe, C:\\...\\app.exe" };
        var args = new TextBox { Header = "Argumentos (opcional)", PlaceholderText = $"Ex.: connect 192.168.15.13:5555  ou  -d \"{Servicos.ServicoAcoes.MarcaPasta}\"" };
        var esperar = new CheckBox { Content = "Esperar e mostrar o resultado (comandos de terminal)" };
        var adicionar = new Button { Content = "Adicionar passo", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var flyout = new Flyout
        {
            Content = new StackPanel { Spacing = 10, Width = 380, Children = { nome, programa, args, esperar, adicionar } },
        };
        adicionar.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(programa.Text)) { programa.Focus(FocusState.Programmatic); return; }
            flyout.Hide();
            _ed.AdicionarPasso(acao, new PassoAcao
            {
                Nome = string.IsNullOrWhiteSpace(nome.Text) ? programa.Text.Trim() : nome.Text.Trim(),
                Caminho = programa.Text.Trim(),
                Args = string.IsNullOrWhiteSpace(args.Text) ? null : args.Text.Trim(),
                Esperar = esperar.IsChecked == true,
            });
        };
        return new Button { Content = "+ Adicionar comando…", Flyout = flyout };
    }

    UIElement ConteudoPaineis()
    {
        var pilha = Pilha(new TextBlock
        {
            Text = "Marque para exibir. A largura é em colunas da grade; painéis que não cabem descem para a linha seguinte.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
        });

        for (int i = 0; i < C.Paineis.Count; i++)
        {
            var painel = C.Paineis[i];
            var especial = EditorConfig.Especiais.FirstOrDefault(e => e.Id == painel.Id).Nome;

            var visivel = new CheckBox { IsChecked = painel.Visivel, MinWidth = 0, VerticalAlignment = VerticalAlignment.Center };
            ToolTipService.SetToolTip(visivel, "Exibir este painel");
            visivel.Click += (_, _) => _ed.DefinirVisivel(painel, visivel.IsChecked == true);

            var titulo = new TextBox
            {
                Text = painel.Titulo ?? "",
                PlaceholderText = especial is not null ? $"{especial} — sem título" : painel.Id,
                VerticalAlignment = VerticalAlignment.Center,
            };
            titulo.TextChanged += (_, _) =>
                _ed.Alterar(c => painel.Titulo = string.IsNullOrWhiteSpace(titulo.Text) ? null : titulo.Text);

            var largura = new NumberBox
            {
                Value = painel.Largura,
                Minimum = 1,
                Maximum = Math.Max(1, C.Tema.Colunas),
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                VerticalAlignment = VerticalAlignment.Center,
            };
            ToolTipService.SetToolTip(largura, "Largura (colunas)");
            largura.ValueChanged += (_, e) =>
            {
                if (!double.IsNaN(e.NewValue)) _ed.Alterar(c => painel.Largura = (int)e.NewValue);
            };

            var linha = Linha(visivel, titulo, largura,
                Icone("\uE70E", "Subir", i > 0, () => _ed.MoverPainel(painel, -1)),
                Icone("\uE70D", "Descer", i < C.Paineis.Count - 1, () => _ed.MoverPainel(painel, +1)),
                Icone("\uE74D", "Remover painel", true, () => _ = RemoverPainel(painel)));
            pilha.Children.Add(linha);
        }

        // "+ Adicionar painel": grupo de apps novo ou um dos painéis especiais que ainda não estão na Home.
        var menu = new MenuFlyout();
        var grupo = new MenuFlyoutItem { Text = "Grupo de apps", Icon = new FontIcon { Glyph = "" } };
        grupo.Click += (_, _) =>
        {
            _grupoApps = _ed.NovoGrupo();
            if (_expApps is not null) _expApps.IsExpanded = true;
        };
        menu.Items.Add(grupo);
        foreach (var (id, nome) in EditorConfig.Especiais.Where(e => !_ed.Tem(e.Id)))
        {
            var item = new MenuFlyoutItem { Text = nome };
            item.Click += (_, _) => _ed.AdicionarEspecial(id);
            menu.Items.Add(item);
        }
        // App ancorado: a janela real do app fica sobre o painel (vários podem ficar lado a lado).
        var ancorados = new MenuFlyoutSubItem { Text = "App ancorado (trabalhar no app dentro da Home)" };
        foreach (var app in new AppAncorado[]
                 {
                     new() { Nome = "IntelliJ IDEA", Editor = "intellij" },
                     new() { Nome = "VS Code", Editor = "vscode" },
                     new() { Nome = "Android Studio", Editor = "androidStudio" },
                     new() { Nome = "Terminal", Caminho = "wt.exe", Processo = "WindowsTerminal" },
                 })
        {
            var item = new MenuFlyoutItem { Text = app.Nome };
            item.Click += (_, _) => _ed.NovoAppAncorado(app);
            ancorados.Items.Add(item);
        }
        ancorados.Items.Add(new MenuFlyoutSeparator());
        var janelaAberta = new MenuFlyoutItem { Text = "Escolher uma janela aberta…", Icon = new FontIcon { Glyph = "" } };
        janelaAberta.Click += (_, _) => EscolherJanelaAberta();
        ancorados.Items.Add(janelaAberta);
        var instalado = new MenuFlyoutItem { Text = "Outro app instalado…", Icon = new FontIcon { Glyph = "" } };
        instalado.Click += (_, _) => EscolherAppInstalado();
        ancorados.Items.Add(instalado);
        menu.Items.Add(ancorados);
        pilha.Children.Add(new DropDownButton { Content = "+ Adicionar painel", Flyout = menu });
        return pilha;
    }

    // Lista as janelas abertas; a escolhida vira um painel ancorado (o processo é descoberto pela própria janela).
    async void EscolherJanelaAberta()
    {
        var janelas = Ancoragem.Janelas();
        var lista = new ListView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 420 };
        foreach (var j in janelas)
        {
            UIElement icone = j.Executavel is { } exe && Icones.De(new AppItem { Caminho = exe }, 24) is { } img
                ? new Image { Source = img, Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center }
                : new FontIcon { Glyph = "", FontSize = 18, VerticalAlignment = VerticalAlignment.Center };
            var textos = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = NomeDoPrograma(j), FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                    new TextBlock { Text = j.Titulo, FontSize = 12, Opacity = 0.7, TextTrimming = TextTrimming.CharacterEllipsis },
                },
            };
            lista.Items.Add(new ListViewItem { Content = Linha(icone, textos, null), Tag = j, Padding = new Thickness(8, 6, 8, 6) });
        }

        var dialogo = new ContentDialog
        {
            XamlRoot = _borda.XamlRoot,
            Title = "Ancorar uma janela aberta",
            Content = janelas.Count == 0
                ? new TextBlock { Text = "Nenhuma janela de programa aberta agora.", TextWrapping = TextWrapping.Wrap }
                : lista,
            PrimaryButtonText = "Ancorar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            RequestedTheme = _borda.ActualTheme,
        };
        lista.SelectionChanged += (_, _) => dialogo.IsPrimaryButtonEnabled = lista.SelectedItem is not null;
        lista.DoubleTapped += (_, _) => { if (lista.SelectedItem is not null) { _escolhida = true; dialogo.Hide(); } };
        _escolhida = false;
        var resultado = await dialogo.ShowAsync();
        if ((resultado != ContentDialogResult.Primary && !_escolhida) || lista.SelectedItem is not ListViewItem { Tag: Ancoragem.JanelaAberta janela })
            return;

        var app = new AppAncorado
        {
            Nome = NomeDoPrograma(janela),
            Processo = janela.Processo,
            Caminho = janela.Processo.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) ? null : janela.Executavel,
        };
        var id = _ed.NovoAppAncorado(app);
        Ancoragem.Ancorar(id, janela.Janela); // a janela vai para o painel novo
    }
    bool _escolhida;

    // Nome amigável: a descrição do executável ("IntelliJ IDEA", "DBeaver"), senão o nome do processo.
    static string NomeDoPrograma(Ancoragem.JanelaAberta j)
    {
        try
        {
            if (j.Executavel is { } exe && System.Diagnostics.FileVersionInfo.GetVersionInfo(exe).FileDescription is { Length: > 0 } d)
                return d.Trim();
        }
        catch (Exception) { }
        return j.Processo;
    }

    // Busca entre os apps instalados; o processo é descoberto na primeira vez que o painel abrir o app.
    async void EscolherAppInstalado()
    {
        ContentDialog? dialogo = null;
        AppItem? escolhido = null;
        var busca = BuscaDeInstalados("App instalado", app =>
        {
            escolhido = app;
            dialogo?.Hide();
        });
        dialogo = new ContentDialog
        {
            XamlRoot = _borda.XamlRoot,
            Title = "Ancorar um app instalado",
            Content = new StackPanel
            {
                Spacing = 8,
                Width = 400,
                Children =
                {
                    busca,
                    new TextBlock
                    {
                        Text = "O painel abre o app e ancora a primeira janela nova que aparecer. Se o app já estiver aberto, " +
                               "use \"Escolher uma janela aberta…\".",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Opacity = 0.7,
                    },
                },
            },
            CloseButtonText = "Cancelar",
            RequestedTheme = _borda.ActualTheme,
        };
        await dialogo.ShowAsync();
        if (escolhido is not { } item) return;
        _ed.NovoAppAncorado(new AppAncorado { Nome = item.Nome, App = item.App, Caminho = item.App is null ? item.Caminho : null });
    }

    async Task RemoverPainel(Painel painel)
    {
        var apps = C.Grupos.TryGetValue(painel.Id, out var lista) ? lista.Count : 0;
        if (apps > 0)
        {
            var dialogo = new ContentDialog
            {
                XamlRoot = _borda.XamlRoot,
                Title = $"Remover \"{_ed.Titulo(painel.Id)}\"?",
                Content = $"O painel e os {apps} apps do grupo saem da Home (o arquivo anterior fica em config.json.bak).",
                PrimaryButtonText = "Remover",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = _borda.ActualTheme,
            };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;
        }
        _ed.RemoverPainel(painel);
    }

    UIElement ConteudoApps()
    {
        var grupos = _ed.Grupos();
        if (grupos.Count == 0)
            return new TextBlock { Text = "Crie um grupo de apps em Painéis.", Opacity = 0.7, TextWrapping = TextWrapping.Wrap };
        if (_grupoApps is null || grupos.All(g => g.Id != _grupoApps)) _grupoApps = grupos[0].Id;
        var grupo = _grupoApps;

        var seletor = new ComboBox { Header = "Grupo", HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var (id, titulo) in grupos) seletor.Items.Add(new ComboBoxItem { Content = titulo, Tag = id });
        seletor.SelectedIndex = grupos.FindIndex(g => g.Id == grupo);
        seletor.SelectionChanged += (_, _) =>
        {
            if (seletor.SelectedItem is ComboBoxItem { Tag: string id } && id != _grupoApps)
            {
                _grupoApps = id;
                if (_expApps is not null) _expApps.Content = ConteudoApps();
            }
        };

        var pilha = Pilha(seletor);
        var apps = C.Grupos[grupo];
        for (int i = 0; i < apps.Count; i++)
        {
            var app = apps[i];
            UIElement figura = Icones.De(app, 24) is { } img
                ? new Image { Source = img, Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Center }
                : new FontIcon { Glyph = "\uE71D", VerticalAlignment = VerticalAlignment.Center };
            var nome = new TextBox { Text = app.Nome, VerticalAlignment = VerticalAlignment.Center };
            ToolTipService.SetToolTip(nome, app.App ?? app.Caminho);
            nome.TextChanged += (_, _) => _ed.Alterar(c => app.Nome = nome.Text);

            pilha.Children.Add(Linha(figura, nome, null,
                Icone("\uE70E", "Subir", i > 0, () => _ed.MoverApp(grupo, app, -1)),
                Icone("\uE70D", "Descer", i < apps.Count - 1, () => _ed.MoverApp(grupo, app, +1)),
                Icone("\uE74D", "Remover do grupo", true, () => _ed.RemoverApp(grupo, app))));
        }

        pilha.Children.Add(BuscaDeInstalados("Adicionar app instalado", app => _ed.AdicionarApp(grupo, app)));
        var arquivo = new Button { Content = "Adicionar programa ou arquivo…" };
        arquivo.Click += async (_, _) =>
        {
            var escolhido = await EscolherArquivo(["*"]);
            if (escolhido is not null)
                _ed.AdicionarApp(grupo, new AppItem { Nome = Path.GetFileNameWithoutExtension(escolhido), Caminho = escolhido });
        };
        pilha.Children.Add(arquivo);
        return pilha;
    }

    // Caixa com sugestões dos apps do menu Iniciar; Enter ou clique entrega o app escolhido.
    UIElement BuscaDeInstalados(string titulo, Action<AppItem> adicionar)
    {
        var caixa = new AutoSuggestBox
        {
            Header = titulo,
            PlaceholderText = "Digite o nome do app…",
            QueryIcon = new SymbolIcon(Symbol.Add),
        };
        caixa.TextChanged += async (s, e) =>
        {
            if (e.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            var termo = s.Text.Trim();
            var todos = await AppsInstalados.Obter();
            _sugestoes = termo.Length == 0
                ? []
                : todos.Where(a => a.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase)).Take(12).ToList();
            s.ItemsSource = _sugestoes.Select(a => a.Nome).ToList();
        };
        caixa.QuerySubmitted += (s, e) =>
        {
            var escolhido = e.ChosenSuggestion is string nome
                ? _sugestoes.FirstOrDefault(a => a.Nome == nome)
                : _sugestoes.FirstOrDefault();
            if (escolhido is null) return;
            s.Text = "";
            s.ItemsSource = null;
            adicionar(escolhido.ParaItem());
        };
        return caixa;
    }

    FrameworkElement SeletorImagem(string atual)
    {
        var caminho = new TextBox { Header = "Imagem de fundo", Text = atual, PlaceholderText = @"C:\...\imagem.jpg" };
        caminho.TextChanged += (_, _) => _ed.Alterar(c => c.Tema.ImagemFundo = caminho.Text);
        var escolher = new Button { Content = "Escolher…", VerticalAlignment = VerticalAlignment.Bottom };
        escolher.Click += async (_, _) =>
        {
            if (await EscolherArquivo([".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"]) is { } arquivo)
                caminho.Text = arquivo; // TextChanged grava
        };
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(escolher, 1);
        grid.Children.Add(caminho);
        grid.Children.Add(escolher);
        return grid;
    }

    async Task<string?> EscolherArquivo(string[] tipos)
    {
        var seletor = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, _hwnd);
        foreach (var tipo in tipos) seletor.FileTypeFilter.Add(tipo);
        var arquivo = await seletor.PickSingleFileAsync();
        return arquivo?.Path;
    }

    // ---------- Blocos de controles ----------

    Expander Secao(string titulo, UIElement conteudo)
    {
        var exp = new Expander
        {
            Header = titulo,
            Content = conteudo,
            IsExpanded = _expandidas.GetValueOrDefault(titulo),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        exp.Expanding += (_, _) => _expandidas[titulo] = true;
        exp.Collapsed += (_, _) => _expandidas[titulo] = false;
        return exp;
    }

    static StackPanel Pilha(params UIElement[] itens)
    {
        var pilha = new StackPanel { Spacing = 12 };
        foreach (var item in itens) pilha.Children.Add(item);
        return pilha;
    }

    // [inicio] [centro, ocupa o resto] [extra] [botões...]
    static Grid Linha(UIElement inicio, UIElement centro, UIElement? extra, params UIElement[] botoes)
    {
        var grid = new Grid { ColumnSpacing = 6 };
        var elementos = new List<UIElement> { inicio, centro };
        if (extra is not null) elementos.Add(extra);
        elementos.AddRange(botoes);
        for (int i = 0; i < elementos.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = i == 1 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto,
            });
            Grid.SetColumn((FrameworkElement)elementos[i], i);
            grid.Children.Add(elementos[i]);
        }
        return grid;
    }

    static Button Icone(string glifo, string dica, bool ativo, Action acao)
    {
        var b = new Button
        {
            Content = new FontIcon { Glyph = glifo, FontSize = 12 },
            Padding = new Thickness(8),
            IsEnabled = ativo,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(b, dica);
        b.Click += (_, _) => acao();
        return b;
    }

    static ToggleSwitch Alternador(string titulo, bool valor, Action<bool> mudou)
    {
        var t = new ToggleSwitch { Header = titulo, IsOn = valor, OnContent = "Ligado", OffContent = "Desligado" };
        t.Toggled += (_, _) => mudou(t.IsOn);
        return t;
    }

    static TextBox Texto(string titulo, string valor, Action<string> mudou, bool aoConfirmar = false, string? exemplo = null)
    {
        var caixa = new TextBox { Header = titulo, Text = valor, PlaceholderText = exemplo ?? "" };
        if (aoConfirmar)
        {
            caixa.LostFocus += (_, _) => mudou(caixa.Text);
            caixa.KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Enter) mudou(caixa.Text);
            };
        }
        else
        {
            caixa.TextChanged += (_, _) => mudou(caixa.Text);
        }
        return caixa;
    }

    static ComboBox Escolha(string titulo, (string Rotulo, string Valor)[] opcoes, string atual, Action<string> mudou)
    {
        var combo = new ComboBox { Header = titulo, HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var (rotulo, valor) in opcoes) combo.Items.Add(new ComboBoxItem { Content = rotulo, Tag = valor });
        combo.SelectedIndex = Math.Max(0, Array.FindIndex(opcoes, o => o.Valor == atual));
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem { Tag: string v }) mudou(v);
        };
        return combo;
    }

    static Slider Deslizante(string titulo, double min, double max, double passo, double valor, Action<double> mudou)
    {
        var s = new Slider
        {
            Header = titulo,
            Minimum = min,
            Maximum = max,
            StepFrequency = passo,
            SmallChange = passo,
            Value = Math.Clamp(valor, min, max),
        };
        s.ValueChanged += (_, e) => mudou(e.NewValue);
        return s;
    }

    static FrameworkElement BotaoCor(string titulo, string hex, Action<string> mudou, bool prontas)
    {
        var cor = Estilo.Cor(hex, Colors.Gray);
        var amostra = new Border { Width = 40, Height = 20, CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(cor) };
        var seletor = new ColorPicker
        {
            Color = cor,
            IsAlphaEnabled = false,
            IsMoreButtonVisible = false,
            IsColorChannelTextInputVisible = false,
        };
        var botao = new Button { Content = amostra, Flyout = new Flyout { Content = seletor } };
        ToolTipService.SetToolTip(botao, "Escolher cor");

        void Definir(Color c, bool doSeletor)
        {
            amostra.Background = new SolidColorBrush(c);
            if (!doSeletor) seletor.Color = c;
            mudou($"#{c.R:X2}{c.G:X2}{c.B:X2}");
        }
        seletor.ColorChanged += (_, e) => Definir(e.NewColor, doSeletor: true);

        var linha = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { botao } };
        if (prontas)
        {
            foreach (var p in CoresProntas)
            {
                var c = Estilo.Cor(p, Colors.Gray);
                var b = new Button
                {
                    Width = 28,
                    Height = 28,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(14),
                    Content = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(9), Background = new SolidColorBrush(c) },
                };
                ToolTipService.SetToolTip(b, p);
                b.Click += (_, _) => Definir(c, doSeletor: false);
                linha.Children.Add(b);
            }
        }

        return new StackPanel { Spacing = 6, Children = { new TextBlock { Text = titulo }, linha } };
    }
}
