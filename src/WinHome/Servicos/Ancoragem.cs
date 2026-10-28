using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace WinHome.Servicos;

// Apps ancorados: a janela real de outro programa fica posicionada sobre a área de um painel e acompanha a Home
// (rolagem, mudança de lugar/tamanho, troca de monitor). Não usa SetParent nem "dono": se o WinHome cair, o app
// continua normal. Com a Home escondida ou minimizada, os apps ancorados são minimizados (nunca escondidos),
// para seguirem acessíveis pela barra de tarefas mesmo se o WinHome for encerrado à força.
public static class Ancoragem
{
    sealed class Item
    {
        public IntPtr Janela;
        public FrameworkElement? Area;  // área do painel (recriada a cada remontagem da Home)
        public Placement Original;      // como a janela estava antes de ancorar
        public bool Minimizada;
        public Ret? Recorte; // região aplicada (coordenadas da janela); null = janela inteira
        public DateTime? SemArea; // desde quando a área do painel não está na tela
        public int AlturaMinima; // menor altura da área que o app aceita (pixels físicos; 0 = ainda não se sabe)
    }

    static readonly Dictionary<string, Item> Itens = [];
    static IntPtr _home;
    static FrameworkElement? _visivel; // área da Home onde os painéis aparecem (sem a busca e a barra de cima)
    static DispatcherQueueTimer? _relogio;

    public static event Action<string>? Mudou; // id cuja janela foi ancorada, solta ou fechada

    public static void Iniciar(IntPtr home, FrameworkElement areaVisivel, DispatcherQueue fila)
    {
        _home = home;
        _visivel = areaVisivel;
        _relogio = fila.CreateTimer();
        _relogio.Interval = TimeSpan.FromMilliseconds(50);
        _relogio.Tick += (_, _) => Acompanhar();
        _relogio.Start();
    }

    public static bool Ancorado(string id) => Itens.TryGetValue(id, out var i) && i.Janela != IntPtr.Zero;

    public static IntPtr Janela(string id) => Itens.TryGetValue(id, out var i) ? i.Janela : IntPtr.Zero;

    public static bool EhAncorada(IntPtr janela) => janela != IntPtr.Zero && Itens.Values.Any(i => i.Janela == janela);

    // Quanto falta (em pixels da Home) para a área do painel chegar à altura mínima do app; negativo = sobra.
    // null = app não ancorado ou mínimo ainda desconhecido (só se descobre quando o app recusa encolher).
    public static double? FaltaNaArea(string id)
    {
        if (!Itens.TryGetValue(id, out var i) || i.Janela == IntPtr.Zero || i.AlturaMinima == 0) return null;
        if (i.Area is not { IsLoaded: true, XamlRoot: { } raiz } area) return null;
        return i.AlturaMinima / raiz.RasterizationScale - area.ActualHeight;
    }

    // A área do painel mudou (Home remontada): a janela passa a seguir a área nova.
    public static void Registrar(string id, FrameworkElement area)
    {
        if (Itens.TryGetValue(id, out var item)) item.Area = area;
        else Itens[id] = new Item { Area = area };
    }

    public static bool Ancorar(string id, IntPtr janela)
    {
        if (janela == IntPtr.Zero || !IsWindow(janela) || EhAncorada(janela)) return false;
        var original = new Placement { Tamanho = Marshal.SizeOf<Placement>() };
        GetWindowPlacement(janela, ref original);
        if (!Itens.TryGetValue(id, out var item)) Itens[id] = item = new Item();
        item.Janela = janela;
        item.Original = original;
        item.Minimizada = false;
        item.AlturaMinima = 0;
        if (IsZoomed(janela) || IsIconic(janela)) ShowWindow(janela, SW_RESTORE); // maximizada não aceita posição
        Mudou?.Invoke(id);
        Acompanhar();
        return true;
    }

    // Devolve a janela como estava antes (tamanho, posição e se estava maximizada).
    public static void Soltar(string id)
    {
        if (!Itens.TryGetValue(id, out var item) || item.Janela == IntPtr.Zero) return;
        var janela = item.Janela;
        item.Janela = IntPtr.Zero;
        if (IsWindow(janela))
        {
            SetWindowRgn(janela, IntPtr.Zero, true);
            item.Recorte = null;
            var original = item.Original;
            if (original.Mostrar == SW_SHOWMINIMIZED) original.Mostrar = SW_SHOWNORMAL;
            SetWindowPlacement(janela, ref original);
        }
        Mudou?.Invoke(id);
    }

    public static void SoltarTodos()
    {
        foreach (var id in Itens.Keys.ToList()) Soltar(id);
    }

    // Home ativada: as janelas ancoradas voltam para cima dela.
    public static void HomeAtivada()
    {
        foreach (var item in Itens.Values.Where(i => i.Janela != IntPtr.Zero && !i.Minimizada)) AcimaDaHome(item.Janela);
    }

    // Coloca a janela logo acima da Home (não no topo de tudo: programas que o usuário trouxe para a frente
    // continuam por cima). Só mexe se ela estiver atrás.
    static void AcimaDaHome(IntPtr janela)
    {
        for (var h = GetWindow(_home, GW_HWNDPREV); h != IntPtr.Zero; h = GetWindow(h, GW_HWNDPREV))
            if (h == janela) return; // já está acima
        var acima = GetWindow(_home, GW_HWNDPREV);
        // Inserir abaixo de uma janela "sempre visível" tornaria a ancorada "sempre visível" também.
        if (acima != IntPtr.Zero && (GetWindowLongPtr(acima, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0) acima = HWND_TOP;
        SetWindowPos(janela, acima, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    static void Acompanhar()
    {
        if (_home == IntPtr.Zero) return;
        var homeVisivel = IsWindowVisible(_home) && !IsIconic(_home);
        foreach (var (id, item) in Itens)
        {
            if (item.Janela == IntPtr.Zero) continue;
            if (!IsWindow(item.Janela)) // o usuário fechou o app
            {
                item.Janela = IntPtr.Zero;
                Mudou?.Invoke(id);
                continue;
            }

            // Painel recém-criado ou Home sendo remontada: a área some por um instante; só minimiza se sumir de vez
            // (painel ocultado pelo ✕, busca na tela).
            if (item.Area is { IsLoaded: true }) item.SemArea = null;
            else
            {
                item.SemArea ??= DateTime.Now;
                if (homeVisivel && DateTime.Now - item.SemArea < TimeSpan.FromMilliseconds(600)) continue;
            }

            var alvo = homeVisivel ? Retangulo(item.Area) : null;
            var visivel = alvo is { } a && _visivel is not null ? Intersecao(a, AreaLivre()) : null;
            if (alvo is null || visivel is null)
            {
                if (!item.Minimizada && !IsIconic(item.Janela)) ShowWindow(item.Janela, SW_SHOWMINNOACTIVE);
                item.Minimizada = true;
                continue;
            }
            if (item.Minimizada || IsIconic(item.Janela) || IsZoomed(item.Janela))
            {
                ShowWindow(item.Janela, SW_SHOWNOACTIVATE);
                if (IsZoomed(item.Janela)) ShowWindow(item.Janela, SW_RESTORE);
                item.Minimizada = false;
            }
            AcimaDaHome(item.Janela);

            // A parte visível da janela (sem as bordas transparentes do Windows 11) cobre exatamente a área do painel.
            var r = alvo.Value;
            GetWindowRect(item.Janela, out var atual);
            var w = r;
            if (DwmGetWindowAttribute(item.Janela, DWMWA_EXTENDED_FRAME_BOUNDS, out Ret moldura, Marshal.SizeOf<Ret>()) == 0)
            {
                w.Esquerda -= Math.Max(0, moldura.Esquerda - atual.Esquerda);
                w.Topo -= Math.Max(0, moldura.Topo - atual.Topo);
                w.Direita += Math.Max(0, atual.Direita - moldura.Direita);
                w.Base += Math.Max(0, atual.Base - moldura.Base);
            }
            if (!atual.Equals(w))
            {
                SetWindowPos(item.Janela, IntPtr.Zero, w.Esquerda, w.Topo, w.Direita - w.Esquerda, w.Base - w.Topo,
                    SWP_NOZORDER | SWP_NOACTIVATE);
                GetWindowRect(item.Janela, out atual);
                // O app recusou ficar tão baixo: essa é a altura mínima dele (a alça da linha da Home não passa disso).
                var sobra = (atual.Base - atual.Topo) - (w.Base - w.Topo);
                if (sobra > 0) item.AlturaMinima = r.Base - r.Topo + sobra;
            }
            var maior = atual.Base - atual.Topo > w.Base - w.Topo || atual.Direita - atual.Esquerda > w.Direita - w.Esquerda;

            // Parte do painel fora da área visível (rolagem, Configurações abertas) ou janela maior que a área (mínimo
            // do app): recorta a janela, para não cobrir os outros painéis.
            var v = visivel.Value;
            Ret? recorte = v.Equals(r) && !maior ? null
                : new Ret { Esquerda = v.Esquerda - w.Esquerda, Topo = v.Topo - w.Topo, Direita = v.Direita - w.Esquerda, Base = v.Base - w.Topo };
            if (!Nullable.Equals(recorte, item.Recorte))
            {
                SetWindowRgn(item.Janela, recorte is { } c ? CreateRectRgn(c.Esquerda, c.Topo, c.Direita, c.Base) : IntPtr.Zero, true);
                item.Recorte = recorte;
            }
        }
    }

    // Área da Home onde a janela pode aparecer: a rolagem dos painéis menos a faixa de cima (botões ⚙ — ❐ ✕ e a
    // alça de arrastar a Home ficam sempre livres).
    static Ret? AreaLivre()
    {
        if (Retangulo(_visivel) is not { } r || _visivel?.XamlRoot is not { } raiz) return null;
        r.Topo += (int)Math.Round(MainWindow.AreaDoTopo * raiz.RasterizationScale);
        return r;
    }

    // Retângulo do elemento na tela, em pixels físicos.
    static Ret? Retangulo(FrameworkElement? e)
    {
        if (e is null || !e.IsLoaded || e.XamlRoot is null || e.ActualWidth < 1 || e.ActualHeight < 1) return null;
        var escala = e.XamlRoot.RasterizationScale;
        var p = e.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
        var origem = new Ponto();
        ClientToScreen(_home, ref origem);
        var x = origem.X + (int)Math.Round(p.X * escala);
        var y = origem.Y + (int)Math.Round(p.Y * escala);
        return new Ret
        {
            Esquerda = x,
            Topo = y,
            Direita = x + (int)Math.Round(e.ActualWidth * escala),
            Base = y + (int)Math.Round(e.ActualHeight * escala),
        };
    }

    static Ret? Intersecao(Ret a, Ret? b)
    {
        if (b is not { } c) return null;
        var r = new Ret
        {
            Esquerda = Math.Max(a.Esquerda, c.Esquerda),
            Topo = Math.Max(a.Topo, c.Topo),
            Direita = Math.Min(a.Direita, c.Direita),
            Base = Math.Min(a.Base, c.Base),
        };
        return r.Direita - r.Esquerda < 20 || r.Base - r.Topo < 20 ? null : r;
    }

    // ---------- Encontrar a janela principal de um app ----------

    // Janela que pode ser ancorada: a principal de um programa (visível, sem dono, com moldura, de bom tamanho).
    public sealed record JanelaAberta(IntPtr Janela, string Processo, string? Executavel, string Titulo, long Area);

    // Janelas principais abertas agora (sem a Home e sem as já ancoradas), da maior para a menor.
    public static List<JanelaAberta> Janelas()
    {
        var lista = new List<JanelaAberta>();
        var processos = new Dictionary<uint, (string Nome, string? Exe)>();
        EnumWindows((h, _) =>
        {
            if (h == _home || !IsWindowVisible(h) || GetWindow(h, GW_OWNER) != IntPtr.Zero || EhAncorada(h) || Oculta(h)) return true;
            var estilo = GetWindowLongPtr(h, GWL_STYLE);
            if ((estilo & (WS_CAPTION | WS_THICKFRAME)) == 0 && !Classe(h).StartsWith("SunAwtFrame")) return true;
            GetWindowRect(h, out var r);
            if (r.Direita - r.Esquerda < 300 || r.Base - r.Topo < 200) return true;
            var titulo = Titulo(h);
            if (titulo.Length == 0) return true;

            GetWindowThreadProcessId(h, out var pid);
            if (pid == Environment.ProcessId) return true;
            if (!processos.TryGetValue(pid, out var p))
            {
                try
                {
                    using var proc = Process.GetProcessById((int)pid);
                    string? exe = null;
                    try { exe = proc.MainModule?.FileName; } catch (Exception) { } // processos protegidos não deixam ler
                    p = (proc.ProcessName, exe);
                }
                catch (ArgumentException) { p = ("", null); }
                processos[pid] = p;
            }
            if (p.Nome.Length == 0) return true;
            lista.Add(new JanelaAberta(h, p.Nome, p.Exe, titulo, (long)(r.Direita - r.Esquerda) * (r.Base - r.Topo)));
            return true;
        }, IntPtr.Zero);
        return lista.OrderByDescending(j => j.Area).ToList();
    }

    // A maior janela principal livre do processo.
    public static IntPtr Procurar(string processo)
    {
        var nome = Path.GetFileNameWithoutExtension(processo);
        return Janelas().FirstOrDefault(j => string.Equals(j.Processo, nome, StringComparison.OrdinalIgnoreCase))?.Janela ?? IntPtr.Zero;
    }

    // Primeira janela principal que não estava aberta antes (para apps cujo processo ainda não se conhece).
    public static JanelaAberta? Nova(IReadOnlySet<IntPtr> antes)
    {
        var novas = Janelas().Where(j => !antes.Contains(j.Janela)).ToList();
        var frente = GetForegroundWindow();
        return novas.FirstOrDefault(j => j.Janela == frente) ?? novas.FirstOrDefault();
    }

    public static string Titulo(IntPtr janela)
    {
        var t = new StringBuilder(256);
        GetWindowText(janela, t, t.Capacity);
        return t.ToString();
    }

    static string Classe(IntPtr h)
    {
        var c = new StringBuilder(128);
        GetClassName(h, c, c.Capacity);
        return c.ToString();
    }

    // Janelas "fantasmas" de apps da Store suspensos ficam visíveis porém ocultas pelo DWM.
    static bool Oculta(IntPtr h) => DwmGetWindowAttribute(h, DWMWA_CLOAKED, out int oculta, sizeof(int)) == 0 && oculta != 0;

    // ---------- Win32 ----------

    [StructLayout(LayoutKind.Sequential)] record struct Ret { public int Esquerda, Topo, Direita, Base; }
    [StructLayout(LayoutKind.Sequential)] struct Ponto { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    struct Placement
    {
        public int Tamanho, Flags, Mostrar;
        public Ponto Minimo, Maximo;
        public Ret Normal;
    }

    delegate bool EnumCb(IntPtr h, IntPtr l);
    const int SW_SHOWNORMAL = 1, SW_SHOWMINIMIZED = 2, SW_SHOWMINNOACTIVE = 7, SW_SHOWNOACTIVATE = 4, SW_RESTORE = 9;
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;
    const uint GW_OWNER = 4, GW_HWNDPREV = 3;
    const long WS_EX_TOPMOST = 0x8;
    const int GWL_EXSTYLE = -20;
    const int GWL_STYLE = -16, DWMWA_EXTENDED_FRAME_BOUNDS = 9, DWMWA_CLOAKED = 14;
    const long WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000;
    static readonly IntPtr HWND_TOP = IntPtr.Zero;

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumCb cb, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern long GetWindowLongPtr(IntPtr h, int i);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out Ret r);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr depois, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref Ponto p);
    [DllImport("user32.dll")] static extern bool GetWindowPlacement(IntPtr h, ref Placement p);
    [DllImport("user32.dll")] static extern bool SetWindowPlacement(IntPtr h, ref Placement p);
    [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr h, IntPtr rgn, bool redesenhar);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int atributo, out int valor, int tamanho);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int atributo, out Ret valor, int tamanho);
}
