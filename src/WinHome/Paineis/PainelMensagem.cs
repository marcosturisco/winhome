using System.Text.Json;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Text;
using WinHome.Configuracao;

namespace WinHome.Paineis;

// Mensagem do dia: uma frase fixa por data; o botão ↻ mostra outra (só nesta sessão).
public static class PainelMensagem
{
    static readonly Lazy<List<Frase>> Embutidas = new(Carregar);
    static int _extra; // quantas vezes o usuário pediu outra frase nesta sessão

    static List<Frase> Carregar()
    {
        try
        {
            var arquivo = Path.Combine(AppContext.BaseDirectory, "Assets", "mensagens.json");
            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Frase>>(File.ReadAllText(arquivo), opcoes) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return [];
        }
    }

    static List<Frase> Lista(Mensagens config)
    {
        var minhas = config.Minhas.Where(f => !string.IsNullOrWhiteSpace(f.Texto)).ToList();
        return config.Fonte switch
        {
            "minhas" => minhas,
            "embutidas" => Embutidas.Value,
            _ => [.. minhas, .. Embutidas.Value],
        };
    }

    public static UIElement Criar(Mensagens config, Estilo estilo)
    {
        var texto = new TextBlock
        {
            FontFamily = estilo.Fonte,
            FontSize = 20,
            FontStyle = FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap,
        };
        var autor = new TextBlock { FontFamily = estilo.Fonte, FontSize = 14, Opacity = 0.75, TextWrapping = TextWrapping.Wrap };

        void Mostrar()
        {
            var lista = Lista(config);
            if (lista.Count == 0)
            {
                texto.Text = "Nenhuma frase disponível. Adicione as suas em Configurações → Mensagens.";
                autor.Text = "";
                return;
            }
            // Mesmo índice durante o dia todo; muda à meia-noite e a cada ↻.
            var dia = DateOnly.FromDateTime(DateTime.Today).DayNumber;
            var frase = lista[(int)(((long)dia * 7919 + _extra) % lista.Count)];
            texto.Text = $"“{frase.Texto}”";
            autor.Text = string.IsNullOrWhiteSpace(frase.Autor) ? "" : $"— {frase.Autor}";
        }
        Mostrar();

        var outra = new Button
        {
            Content = new FontIcon { Glyph = "", FontSize = 14 },
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        ToolTipService.SetToolTip(outra, "Outra mensagem");
        outra.Click += (_, _) => { _extra++; Mostrar(); };

        var rodape = new Grid();
        rodape.ColumnDefinitions.Add(new ColumnDefinition());
        rodape.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(outra, 1);
        autor.VerticalAlignment = VerticalAlignment.Center;
        rodape.Children.Add(autor);
        rodape.Children.Add(outra);

        return new StackPanel { Spacing = 12, Children = { texto, rodape } };
    }
}
