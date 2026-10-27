using System.Diagnostics;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinHome.Configuracao;
using WinHome.Servicos;

namespace WinHome.Paineis;

// Curiosidades do dia: um fato da data de hoje com foto; ↻ mostra outro (só nesta sessão).
public static class PainelCuriosidade
{
    static int _extra; // quantas vezes o usuário pediu outro fato nesta sessão

    // "outro" = botão ↻ do cabeçalho do cartão (lugar fixo, criado pelo HomeBuilder).
    public static UIElement Criar(Curiosidades opcoes, Estilo estilo, Button outro)
    {
        var foto = new Border
        {
            Width = 132,
            Height = 132,
            CornerRadius = new CornerRadius(Math.Max(0, estilo.Tema.Cantos * 0.75)),
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
        };
        var quando = new TextBlock { FontFamily = estilo.Fonte, FontSize = 13, Foreground = estilo.Destaque, FontWeight = FontWeights.SemiBold };
        var texto = new TextBlock { FontFamily = estilo.Fonte, FontSize = 16, TextWrapping = TextWrapping.Wrap, Text = "Carregando…" };
        var ler = new HyperlinkButton { Content = "Ler na Wikipédia", Padding = new Thickness(0), Visibility = Visibility.Collapsed };

        List<Fato> fatos = [];
        string? link = null;
        ler.Click += (_, _) =>
        {
            if (link is not null) Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        };

        void Mostrar()
        {
            if (fatos.Count == 0) return;
            // Mesmo fato o dia todo (destaques primeiro); muda a cada ↻.
            var fato = fatos[_extra % fatos.Count];
            var anos = DateTime.Today.Year - fato.Ano;
            var tipo = fato.Tipo switch { "nascimento" => "Nasceu", "morte" => "Morreu", _ => "Neste dia" };
            quando.Text = anos > 0 ? $"{tipo} em {fato.Ano} · há {anos} {(anos == 1 ? "ano" : "anos")}" : $"{tipo} em {fato.Ano}";
            texto.Text = fato.Tipo is "nascimento" or "morte" && fato.Titulo is not null ? $"{fato.Titulo}: {fato.Texto}" : fato.Texto;
            link = fato.Link;
            ler.Visibility = link is null ? Visibility.Collapsed : Visibility.Visible;
            ToolTipService.SetToolTip(ler, fato.Titulo);

            if (fato.Imagem is { } url && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var bitmap = new BitmapImage(uri) { DecodePixelWidth = 264 };
                bitmap.ImageFailed += (_, _) => foto.Visibility = Visibility.Collapsed; // sem conexão: só o texto
                foto.Background = new ImageBrush { ImageSource = bitmap, Stretch = Stretch.UniformToFill };
                foto.Visibility = Visibility.Visible;
            }
            else
            {
                foto.Visibility = Visibility.Collapsed;
            }
        }

        async void Carregar()
        {
            var r = await ServicoCuriosidades.Obter(DateTime.Today);
            if (r is null)
            {
                texto.Text = "Sem conexão com a Wikipédia. As curiosidades aparecem quando a internet voltar.";
                return;
            }
            fatos = r.Valor.Where(f => f.Tipo switch
            {
                "nascimento" => opcoes.IncluirNascimentos,
                "morte" => opcoes.IncluirMortes,
                _ => true,
            }).ToList();
            if (fatos.Count == 0)
            {
                texto.Text = "Nenhuma curiosidade para hoje.";
                return;
            }
            Mostrar();
        }

        outro.Click += (_, _) => { _extra++; Mostrar(); };


        var fonte = new TextBlock { Text = "Fonte: Wikipédia (CC BY-SA)", FontSize = 11, Opacity = 0.5, FontFamily = estilo.Fonte };
        var direita = new StackPanel { Spacing = 8, Children = { quando, texto, ler, fonte } };

        var grade = new Grid { ColumnSpacing = 16 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(direita, 1);
        grade.Children.Add(foto);
        grade.Children.Add(direita);
        grade.Loaded += (_, _) => Carregar();
        return grade;
    }
}
