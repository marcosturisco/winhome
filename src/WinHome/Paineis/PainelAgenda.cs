using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Agenda: compromissos de hoje e dos próximos dias de cada calendário (cor própria), do Google Agenda.
// Os que já passaram ficam apagados; o atual mostra "agora" e o próximo "em 25 min". Clique abre o dia no Google.
public static class PainelAgenda
{
    static readonly CultureInfo PtBr = new("pt-BR");

    // "atualizar" = botão ↻ do cabeçalho (força nova leitura).
    public static UIElement Criar(Agenda cfg, Estilo estilo, Button atualizar)
    {
        var lista = new StackPanel { Spacing = 4 };
        var aviso = new TextBlock { FontSize = 12, Opacity = 0.6, TextWrapping = TextWrapping.Wrap, FontFamily = estilo.Fonte };
        var calendarios = cfg.Calendarios.Where(c => !string.IsNullOrWhiteSpace(c.Endereco)).ToList();
        if (calendarios.Count == 0)
            return new TextBlock
            {
                Text = "Cole o \"Endereço secreto no formato iCal\" do Google Agenda em Configurações → Agenda.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontFamily = estilo.Fonte,
            };

        List<(Compromisso C, Calendario Cal)> todos = [];
        var dias = Math.Clamp(cfg.Dias, 1, 14);

        void Mostrar()
        {
            lista.Children.Clear();
            var agora = DateTime.Now;
            for (var d = 0; d < dias; d++)
            {
                var dia = DateTime.Today.AddDays(d);
                var doDia = todos.Where(x => x.C.Inicio.Date <= dia && x.C.Fim > dia).ToList();
                lista.Children.Add(new TextBlock
                {
                    Text = d switch { 0 => "Hoje", 1 => "Amanhã", _ => PtBr.TextInfo.ToTitleCase(dia.ToString("dddd", PtBr)) } +
                           "  ·  " + dia.ToString("d 'de' MMMM", PtBr),
                    FontFamily = estilo.Fonte,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Opacity = 0.8,
                    Margin = new Thickness(0, d == 0 ? 0 : 10, 0, 2),
                });
                if (doDia.Count == 0)
                {
                    lista.Children.Add(new TextBlock { Text = "Nada na agenda.", Opacity = 0.5, FontFamily = estilo.Fonte, FontSize = 13, Margin = new Thickness(12, 0, 0, 0) });
                    continue;
                }
                var proximo = d == 0 ? doDia.Where(x => !x.C.DiaTodo && x.C.Inicio > agora).Select(x => x.C).FirstOrDefault() : null;
                foreach (var (c, cal) in doDia) lista.Children.Add(Linha(c, cal, dia, agora, c == proximo, estilo));
            }
        }

        async void Carregar(bool forcar)
        {
            atualizar.IsEnabled = false;
            var novos = new List<(Compromisso, Calendario)>();
            var semRede = false;
            DateTime? maisAntigo = null;
            foreach (var cal in calendarios)
            {
                var r = await ServicoAgenda.Obter(cal.Endereco, dias, forcar);
                if (r is null) { semRede = true; continue; }
                if (!r.Atual) semRede = true;
                if (maisAntigo is null || r.Quando < maisAntigo) maisAntigo = r.Quando;
                novos.AddRange(r.Valor.Select(c => (c, cal)));
            }
            todos = novos.OrderBy(x => x.Item1.Inicio.Date).ThenBy(x => !x.Item1.DiaTodo).ThenBy(x => x.Item1.Inicio).ToList();
            Mostrar();
            aviso.Text = semRede
                ? (maisAntigo is { } q ? $"Sem conexão com o Google Agenda — mostrando o que foi lido às {q:HH:mm}." : "Sem conexão com o Google Agenda.")
                : maisAntigo is { } t ? $"Atualizado às {t:HH:mm}." : "";
            atualizar.IsEnabled = true;
        }

        atualizar.Click += (_, _) => Carregar(forcar: true);

        // A cada minuto: "em X min"/"agora" andam; a leitura em si respeita os 15 min do cache.
        var relogio = lista.DispatcherQueue.CreateTimer();
        relogio.Interval = TimeSpan.FromMinutes(1);
        relogio.Tick += (_, _) => Carregar(forcar: false);
        lista.Loaded += (_, _) => { Carregar(forcar: false); relogio.Start(); };
        lista.Unloaded += (_, _) => relogio.Stop();
        return new StackPanel { Spacing = 8, Children = { lista, aviso } };
    }

    static Button Linha(Compromisso c, Calendario cal, DateTime dia, DateTime agora, bool proximo, Estilo estilo)
    {
        var acontecendo = !c.DiaTodo && c.Inicio <= agora && c.Fim > agora;
        var passou = c.Fim <= agora;

        string Hora(DateTime t) => t.ToString("HH:mm", PtBr);
        var horario = c.DiaTodo ? "Dia todo"
            : c.Inicio.Date < dia ? $"até {Hora(c.Fim)}"            // começou em outro dia
            : c.Fim.Date > dia ? $"{Hora(c.Inicio)} →"                 // termina em outro dia
            : $"{Hora(c.Inicio)} – {Hora(c.Fim)}";

        var selo = acontecendo ? "agora"
            : proximo && (c.Inicio - agora) is var falta && falta.TotalMinutes < 120
                ? (falta.TotalMinutes < 1 ? "já" : $"em {(int)Math.Ceiling(falta.TotalMinutes)} min")
                : null;

        var barra = new Border
        {
            Width = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Estilo.Cor(cal.Cor, Windows.UI.Color.FromArgb(255, 79, 140, 255))),
        };
        var titulo = new TextBlock
        {
            Text = c.Titulo,
            FontFamily = estilo.Fonte,
            FontWeight = acontecendo ? FontWeights.SemiBold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var detalhes = new TextBlock
        {
            Text = c.Local is { } local ? $"{horario}  ·  {local}" : horario,
            FontFamily = estilo.Fonte,
            FontSize = 12,
            Opacity = 0.7,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var textos = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Children = { titulo, detalhes } };

        var grade = new Grid { ColumnSpacing = 10 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textos, 1);
        grade.Children.Add(barra);
        grade.Children.Add(textos);
        if (selo is not null)
        {
            var etiqueta = new Border
            {
                Background = estilo.Destaque,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = selo, FontSize = 12, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), FontFamily = estilo.Fonte },
            };
            Grid.SetColumn(etiqueta, 2);
            grade.Children.Add(etiqueta);
        }

        var botao = new Button
        {
            Content = grade,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 5, 8, 5),
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.5)),
            Opacity = passou ? 0.45 : 1,
        };
        ToolTipService.SetToolTip(botao, $"{cal.Nome}: {c.Titulo}\n{horario}{(c.Local is { } l ? "\n" + l : "")}\nClique para abrir o dia no Google Agenda");
        botao.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo($"https://calendar.google.com/calendar/r/day/{dia:yyyy'/'MM'/'dd}") { UseShellExecute = true });
            }
            catch (Exception) { }
        };
        return botao;
    }
}
