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
    Expander? _expPaineis, _expApps, _expMensagens, _expBoasVindas, _expCuriosidades, _expProjetos, _expTarefas;
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
            if (_expApps is not null) _expApps.Content = ConteudoApps();
            if (_expMensagens is not null) _expMensagens.Content = ConteudoMensagens();
            if (_expBoasVindas is not null) _expBoasVindas.Content = ConteudoBoasVindas();
            if (_expCuriosidades is not null) _expCuriosidades.Content = ConteudoCuriosidades();
            if (_expProjetos is not null) _expProjetos.Content = ConteudoProjetos();
            if (_expTarefas is not null) _expTarefas.Content = ConteudoTarefas();
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
        _secoes.Children.Add(Secao("Aparência", ConteudoAparencia()));
        _secoes.Children.Add(_expBoasVindas = Secao("Boas-vindas", ConteudoBoasVindas()));
        _secoes.Children.Add(_expMensagens = Secao("Mensagens", ConteudoMensagens()));
        _secoes.Children.Add(_expCuriosidades = Secao("Curiosidades", ConteudoCuriosidades()));
        _secoes.Children.Add(_expProjetos = Secao("Projetos", ConteudoProjetos()));
        _secoes.Children.Add(_expTarefas = Secao("Tarefas", ConteudoTarefas()));
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
        pilha.Children.Add(new DropDownButton { Content = "+ Adicionar painel", Flyout = menu });
        return pilha;
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

        pilha.Children.Add(BuscaDeInstalados(grupo));
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

    // Caixa com sugestões dos apps do menu Iniciar; Enter ou clique adiciona ao grupo.
    UIElement BuscaDeInstalados(string grupo)
    {
        var caixa = new AutoSuggestBox
        {
            Header = "Adicionar app instalado",
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
            _ed.AdicionarApp(grupo, escolhido.ParaItem());
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
