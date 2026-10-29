using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Text;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Tarefas: Enter adiciona, marcar conclui (vai riscada para o fim), ✕ remove; botão direito edita ou move.
public static class PainelTarefas
{
    public static UIElement Criar(Tarefas cfg, Estilo estilo)
    {
        var entrada = new TextBox { PlaceholderText = "Nova tarefa… (Enter adiciona)", FontFamily = estilo.Fonte };
        var lista = new StackPanel { Spacing = 2 };
        var aviso = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = estilo.Legenda, Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed) };
        var resumo = new TextBlock { FontFamily = estilo.Fonte, FontSize = estilo.Legenda, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
        var limpar = new Button { Content = "Limpar concluídas", FontSize = estilo.Legenda, Padding = new Thickness(8, 4, 8, 4) };

        void Mostrar()
        {
            var todas = ServicoTarefas.Lista;
            var pendentes = todas.Count(t => !t.Feita);
            var feitas = todas.Count - pendentes;

            lista.Children.Clear();
            foreach (var t in todas.Where(t => !t.Feita || cfg.MostrarConcluidas))
                lista.Children.Add(Linha(t, estilo, Mostrar));
            if (todas.Count == 0)
                lista.Children.Add(new TextBlock { Text = "Nenhuma tarefa. Escreva acima e tecle Enter.", Opacity = 0.6, FontFamily = estilo.Fonte });

            resumo.Text = (pendentes == 1 ? "1 pendente" : $"{pendentes} pendentes") +
                          (feitas > 0 ? $"  ·  {feitas} concluída{(feitas == 1 ? "" : "s")}" : "");
            limpar.Visibility = feitas > 0 ? Visibility.Visible : Visibility.Collapsed;
            aviso.Text = ServicoTarefas.Erro ?? "";
            aviso.Visibility = ServicoTarefas.Erro is null ? Visibility.Collapsed : Visibility.Visible;
        }

        entrada.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Enter)
            {
                ServicoTarefas.Adicionar(entrada.Text);
                entrada.Text = "";
                Mostrar();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Escape && entrada.Text.Length > 0)
            {
                entrada.Text = ""; // o Esc seguinte esconde a Home, como de costume
                e.Handled = true;
            }
        };
        limpar.Click += (_, _) =>
        {
            ServicoTarefas.LimparConcluidas();
            Mostrar();
        };
        Mostrar();

        var rodape = new Grid();
        rodape.ColumnDefinitions.Add(new ColumnDefinition());
        rodape.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(limpar, 1);
        rodape.Children.Add(resumo);
        rodape.Children.Add(limpar);

        var rolagem = new ScrollViewer { Content = lista, MaxHeight = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return new StackPanel { Spacing = 10, Children = { entrada, aviso, rolagem, rodape } };
    }

    static Grid Linha(Tarefa t, Estilo estilo, Action mostrar)
    {
        var texto = new TextBlock
        {
            Text = t.Texto,
            FontFamily = estilo.Fonte,
            TextWrapping = TextWrapping.Wrap,
            Opacity = t.Feita ? 0.55 : 1,
            TextDecorations = t.Feita ? TextDecorations.Strikethrough : TextDecorations.None,
        };
        var caixa = new CheckBox { IsChecked = t.Feita, Content = texto, MinWidth = 0 };
        caixa.Click += (_, _) =>
        {
            ServicoTarefas.Marcar(t, caixa.IsChecked == true);
            mostrar();
        };

        var remover = new Button
        {
            Content = new FontIcon { Glyph = "", FontSize = estilo.Legenda },
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6),
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(remover, "Remover");
        remover.Click += (_, _) =>
        {
            ServicoTarefas.Remover(t);
            mostrar();
        };

        var linha = new Grid();
        linha.ColumnDefinitions.Add(new ColumnDefinition());
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(remover, 1);
        linha.Children.Add(caixa);
        linha.Children.Add(remover);

        // Edição no lugar: Enter ou sair da caixa grava; Esc desiste.
        void Editar()
        {
            var edicao = new TextBox { Text = t.Texto, FontFamily = estilo.Fonte, TextWrapping = TextWrapping.Wrap };
            var terminou = false;
            void Terminar(bool gravar)
            {
                if (terminou) return;
                terminou = true;
                if (gravar) ServicoTarefas.Renomear(t, edicao.Text);
                mostrar();
            }
            edicao.KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Enter) { Terminar(true); e.Handled = true; }
                else if (e.Key == VirtualKey.Escape) { Terminar(false); e.Handled = true; }
            };
            edicao.LostFocus += (_, _) => Terminar(true);
            linha.Children.Remove(caixa);
            linha.Children.Add(edicao);
            edicao.Focus(FocusState.Programmatic);
            edicao.SelectAll();
        }
        texto.DoubleTapped += (_, e) => { Editar(); e.Handled = true; };

        var menu = new MenuFlyout();
        void Item(string rotulo, string glifo, Action acao)
        {
            var item = new MenuFlyoutItem { Text = rotulo, Icon = new FontIcon { Glyph = glifo } };
            item.Click += (_, _) => acao();
            menu.Items.Add(item);
        }
        Item("Editar", "", Editar);
        Item("Mover para cima", "", () => { ServicoTarefas.Mover(t, -1); mostrar(); });
        Item("Mover para baixo", "", () => { ServicoTarefas.Mover(t, 1); mostrar(); });
        menu.Items.Add(new MenuFlyoutSeparator());
        Item("Remover", "", () => { ServicoTarefas.Remover(t); mostrar(); });
        linha.ContextFlyout = menu;
        return linha;
    }
}
