using Microsoft.UI.Xaml;

namespace WinHome.Servicos;

// A Home está na tela? (janela visível e não minimizada; no modo área de trabalho, atrás das janelas conta como
// visível.) Os painéis usam isto para não trabalhar à toa com a Home escondida.
public static class EstadoHome
{
    public static bool Visivel { get; private set; } = true;

    public static event Action<bool>? Mudou; // na thread da interface

    public static void Definir(bool visivel)
    {
        if (visivel == Visivel) return;
        Visivel = visivel;
        Mudou?.Invoke(visivel);
    }

    // Liga o trabalho periódico de um painel (relógios, leituras) só enquanto ele está na Home e a Home está na tela;
    // "ligar" roda também ao voltar, então deve atualizar na hora o que ficou velho.
    public static void Vincular(FrameworkElement elemento, Action ligar, Action desligar)
    {
        var carregado = false;
        var ligado = false;
        void Ajustar()
        {
            var deve = carregado && Visivel;
            if (deve == ligado) return;
            ligado = deve;
            if (deve) ligar();
            else desligar();
        }
        void AoMudar(bool _) => Ajustar();
        elemento.Loaded += (_, _) =>
        {
            if (!carregado) Mudou += AoMudar;
            carregado = true;
            Ajustar();
        };
        elemento.Unloaded += (_, _) =>
        {
            if (carregado) Mudou -= AoMudar;
            carregado = false;
            Ajustar();
        };
    }
}
