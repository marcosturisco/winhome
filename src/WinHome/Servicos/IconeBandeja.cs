using System.Runtime.InteropServices;

namespace WinHome.Servicos;

// Ícone do WinHome na bandeja do sistema: clique mostra a Home; botão direito abre o menu.
public sealed class IconeBandeja : IDisposable
{
    const uint WM_APP_BANDEJA = 0x8000 + 1; // WM_APP + 1
    const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIM_SETVERSION = 4;
    const uint NIF_MESSAGE = 0x1, NIF_ICON = 0x2, NIF_TIP = 0x4, NIF_INFO = 0x10, NIF_SHOWTIP = 0x80;
    const uint NIIF_WARNING = 0x2;
    const uint NOTIFYICON_VERSION_4 = 4;
    const uint NIN_SELECT = 0x400, NIN_KEYSELECT = 0x401, WM_CONTEXTMENU = 0x7B;
    const uint NIN_BALLOONHIDE = 0x403, NIN_BALLOONTIMEOUT = 0x404, NIN_BALLOONUSERCLICK = 0x405;
    const uint MF_STRING = 0, MF_SEPARATOR = 0x800, TPM_RIGHTBUTTON = 0x2, TPM_RETURNCMD = 0x100;
    const uint ID_MOSTRAR = 1, ID_CONFIGURACOES = 2, ID_SAIR = 3;

    readonly IntPtr _hwnd;
    readonly IntPtr _icone;
    readonly uint _taskbarCreated;
    bool _visivel;
    bool _desejado; // ícone ligado na config (sem ele, aparece só enquanto um aviso está na tela)
    bool _avisando;
    string _dica = "WinHome";

    public event Action? Mostrar;
    public event Action? Configuracoes;
    public event Action? Sair;

    public IconeBandeja(MensagensJanela mensagens, string arquivoIcone)
    {
        _hwnd = mensagens.Hwnd;
        _icone = LoadImage(IntPtr.Zero, arquivoIcone, 1 /* IMAGE_ICON */,
            GetSystemMetrics(49 /* SM_CXSMICON */), GetSystemMetrics(50 /* SM_CYSMICON */), 0x10 /* LR_LOADFROMFILE */);
        // O Explorer avisa quando reinicia; aí o ícone precisa ser recriado.
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        mensagens.Adicionar(Tratar);
    }

    // Liga/desliga o ícone e atualiza o texto da dica.
    public void Definir(bool visivel, string dica)
    {
        _dica = dica.Length > 127 ? dica[..127] : dica;
        _desejado = visivel;
        if (visivel && !_visivel) Adicionar();
        else if (visivel) Chamar(NIM_MODIFY);
        else if (_visivel && !_avisando) Remover();
    }

    // Notificação do Windows (balão da bandeja, que o Windows 11 mostra como notificação); clicar nela mostra a Home.
    public void Avisar(string titulo, string texto)
    {
        if (!_visivel) Adicionar();
        if (!_visivel) return;
        _avisando = true;
        var dados = Dados();
        dados.uFlags |= NIF_INFO;
        dados.szInfoTitle = titulo.Length > 63 ? titulo[..63] : titulo;
        dados.szInfo = texto.Length > 255 ? texto[..255] : texto;
        dados.dwInfoFlags = NIIF_WARNING;
        Shell_NotifyIcon(NIM_MODIFY, ref dados);
    }

    void AvisoTerminou()
    {
        _avisando = false;
        if (!_desejado && _visivel) Remover();
    }

    void Adicionar()
    {
        _visivel = Chamar(NIM_ADD);
        if (_visivel)
        {
            var dados = Dados();
            dados.uVersion = NOTIFYICON_VERSION_4;
            Shell_NotifyIcon(NIM_SETVERSION, ref dados);
        }
    }

    void Remover()
    {
        Chamar(NIM_DELETE);
        _visivel = false;
    }

    bool Chamar(uint operacao)
    {
        var dados = Dados();
        return Shell_NotifyIcon(operacao, ref dados);
    }

    NOTIFYICONDATA Dados() => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
        hWnd = _hwnd,
        uID = 1,
        uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP,
        uCallbackMessage = WM_APP_BANDEJA,
        hIcon = _icone,
        szTip = _dica,
        szInfo = "",
        szInfoTitle = "",
    };

    bool Tratar(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == _taskbarCreated && _visivel)
        {
            _visivel = false;
            Adicionar();
            return false;
        }
        if (msg != WM_APP_BANDEJA) return false;

        // Versão 4: evento no word baixo do lParam; posição do clique no wParam.
        var evento = (uint)(lParam.ToInt64() & 0xFFFF);
        if (evento is NIN_SELECT or NIN_KEYSELECT) Mostrar?.Invoke();
        else if (evento is NIN_BALLOONHIDE or NIN_BALLOONTIMEOUT) AvisoTerminou();
        else if (evento == NIN_BALLOONUSERCLICK)
        {
            AvisoTerminou();
            Mostrar?.Invoke();
        }
        else if (evento == WM_CONTEXTMENU)
        {
            var w = wParam.ToInt64();
            Menu((short)(w & 0xFFFF), (short)((w >> 16) & 0xFFFF));
        }
        return true;
    }

    void Menu(int x, int y)
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, ID_MOSTRAR, "Mostrar a Home");
        AppendMenu(menu, MF_STRING, ID_CONFIGURACOES, "Configurações");
        AppendMenu(menu, MF_SEPARATOR, 0, null);
        AppendMenu(menu, MF_STRING, ID_SAIR, "Sair do WinHome");

        // Sem isso o menu não fecha ao clicar fora dele.
        SetForegroundWindow(_hwnd);
        var escolha = TrackPopupMenuEx(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, x, y, _hwnd, IntPtr.Zero);
        DestroyMenu(menu);
        PostMessage(_hwnd, 0 /* WM_NULL */, IntPtr.Zero, IntPtr.Zero);

        switch (escolha)
        {
            case ID_MOSTRAR: Mostrar?.Invoke(); break;
            case ID_CONFIGURACOES: Configuracoes?.Invoke(); break;
            case ID_SAIR: Sair?.Invoke(); break;
        }
    }

    public void Dispose()
    {
        if (_visivel) Remover();
        if (_icone != IntPtr.Zero) DestroyIcon(_icone);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
    static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATA data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr LoadImage(IntPtr hinst, string nome, uint tipo, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int indice);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icone);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string nome);
    [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string? texto);
    [DllImport("user32.dll")] static extern uint TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr tpm);
    [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
}
