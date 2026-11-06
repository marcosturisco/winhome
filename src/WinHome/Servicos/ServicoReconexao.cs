using System.Runtime.InteropServices;

namespace WinHome.Servicos;

// Reconectar um aparelho de áudio Bluetooth (fone, caixa de som, celular) sem administrador: o mesmo comando que o
// Windows usa no "Conectar"/"Desconectar" das configurações de Som — uma propriedade do driver de áudio Bluetooth
// (KSPROPSETID_BtAudio), pedida pela saída de áudio do aparelho. Como não existe um "reconectar" único, desconecta,
// espera 5 s e conecta de novo. Mouse e teclado (Bluetooth LE) não têm esse comando: o Windows só os desconecta
// desativando o dispositivo (administrador) ou removendo o pareamento — por isso ficam sem o botão.
// Os aparelhos são achados pelo "container" (o mesmo ID que agrupa as entradas no ServicoBaterias).
// Testado neste PC (06/11/2026), com o áudio Bluetooth "offload" da Intel: o filtro de música (render) recusa o
// comando (0x80070001) e o de viva-voz (capture) aceita — e derruba/religa o fone inteiro, música junto. Por isso o
// comando vai a todos os filtros do aparelho e basta um aceitar. O viva-voz desconectado fica "ausente" (estado 0x4):
// sem incluir esse estado, o "conectar" não o achava e o microfone ficava desligado.
public static class ServicoReconexao
{
    // Aparelhos (container) com o comando em andamento ("Desconectando…") ou que acabaram de falhar ("Falhou: …").
    static readonly Dictionary<Guid, string> _andamento = [];
    static readonly HashSet<Guid> _ocupados = [];

    public static event Action? Mudou; // na thread da interface

    public static string? Andamento(Guid container) => _andamento.TryGetValue(container, out var t) ? t : null;
    public static bool Ocupado(Guid container) => _ocupados.Contains(container);

    // Containers que têm saída de áudio Bluetooth com o comando disponível (conectados ou não).
    public static HashSet<Guid> ContainersDeAudio()
    {
        var lista = new HashSet<Guid>();
        try
        {
            foreach (var (container, ks) in Filtros(null))
            {
                lista.Add(container);
                Marshal.ReleaseComObject(ks);
            }
        }
        catch (Exception) { } // sem áudio/COM: nenhum aparelho ganha o botão
        return lista;
    }

    // Desconecta (se conectado), espera 5 s e conecta. Chamar na thread da interface.
    public static async Task Reconectar(Guid container, bool conectado)
    {
        if (!_ocupados.Add(container)) return;
        string? erro = null;
        try
        {
            if (conectado)
            {
                Avisar(container, "Desconectando…");
                erro = await Task.Run(() => Enviar(container, Desconectar));
                if (erro is null)
                {
                    Avisar(container, "Desconectado · conectando de novo em 5 s…");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            if (erro is null)
            {
                Avisar(container, "Conectando…");
                erro = await Task.Run(() => Enviar(container, Conectar));
            }
        }
        catch (Exception ex) { erro = ex.Message; }
        finally { _ocupados.Remove(container); }

        if (erro is null) _andamento.Remove(container);
        else _andamento[container] = "Falhou: " + erro;
        Mudou?.Invoke();
        _ = ServicoBaterias.AtualizarAgora();
        if (erro is null) return;
        await Task.Delay(TimeSpan.FromSeconds(20)); // o aviso de falha some sozinho
        if (_ocupados.Contains(container)) return;
        _andamento.Remove(container);
        Mudou?.Invoke();
    }

    static void Avisar(Guid container, string texto)
    {
        _andamento[container] = texto;
        Mudou?.Invoke();
    }

    const uint Conectar = 0;    // KSPROPERTY_ONESHOT_RECONNECT
    const uint Desconectar = 1; // KSPROPERTY_ONESHOT_DISCONNECT

    // Manda o comando a cada filtro de áudio do aparelho (A2DP e viva-voz são filtros separados).
    static string? Enviar(Guid container, uint comando)
    {
        var filtros = Filtros(container);
        if (filtros.Count == 0) return "o Windows não tem uma saída de áudio para este aparelho.";
        var falhas = 0;
        var codigo = 0;
        foreach (var (_, ks) in filtros)
        {
            try
            {
                var prop = new KsProperty { Set = KsPropSetIdBtAudio, Id = comando, Flags = KsPropertyTypeGet };
                var hr = ks.KsProperty(ref prop, (uint)Marshal.SizeOf<KsProperty>(), IntPtr.Zero, 0, out _);
                if (hr < 0) { falhas++; codigo = hr; }
            }
            finally { Marshal.ReleaseComObject(ks); }
        }
        return falhas == filtros.Count ? $"o Windows recusou o comando (0x{codigo:X8})." : null;
    }

    // Filtros do driver de áudio Bluetooth por trás das saídas de áudio (de um container, ou de todos com null).
    // O filtro pode ser o driver Bluetooth da Microsoft (BTHENUM) ou o de áudio da placa, quando o som Bluetooth
    // passa por ela ("offload", ex.: Intel); quem não aceita o comando é descartado ao enviar.
    static List<(Guid Container, IKsControl Ks)> Filtros(Guid? container)
    {
        var achados = new List<(Guid, IKsControl)>();
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enumerador = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        try
        {
            Marshal.ThrowExceptionForHR(enumerador.EnumAudioEndpoints(2 /* eAll */, 0x1 | 0x4 | 0x8 /* ativo, ausente e desligado (perfil desconectado some como "ausente") */, out var colecao));
            colecao.GetCount(out var total);
            for (uint i = 0; i < total; i++)
            {
                if (colecao.Item(i, out var saida) < 0) continue;
                try
                {
                    var c = ContainerDe(saida);
                    if (c is null || c == SemContainer || (container is { } alvo && c != alvo)) continue;
                    if (IdDoFiltro(saida) is not { } id || !vistos.Add(id)) continue;
                    if (enumerador.GetDevice(id, out var filtro) < 0) continue;
                    try
                    {
                        var iidKs = typeof(IKsControl).GUID;
                        var hr = filtro.Activate(ref iidKs, 0x17 /* CLSCTX_ALL */, IntPtr.Zero, out var ks);
                        if (hr >= 0) achados.Add((c.Value, (IKsControl)ks));
                    }
                    finally { Marshal.ReleaseComObject(filtro); }
                }
                catch (Exception) { } // uma saída com problema não impede as outras
                finally { Marshal.ReleaseComObject(saida); }
            }
            Marshal.ReleaseComObject(colecao);
        }
        finally { Marshal.ReleaseComObject(enumerador); }
        return achados;
    }

    static readonly Guid SemContainer = new("00000000-0000-0000-FFFF-FFFFFFFFFFFF"); // aparelhos internos do PC

    static Guid? ContainerDe(IMMDevice saida)
    {
        if (saida.OpenPropertyStore(0 /* STGM_READ */, out var props) < 0) return null;
        try
        {
            var chave = new PropertyKey { Fmtid = new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), Pid = 2 };
            if (props.GetValue(ref chave, out var valor) < 0) return null;
            try { return valor.Vt == 72 /* VT_CLSID */ && valor.Ponteiro != IntPtr.Zero ? Marshal.PtrToStructure<Guid>(valor.Ponteiro) : null; }
            finally { PropVariantClear(ref valor); }
        }
        finally { Marshal.ReleaseComObject(props); }
    }

    // Saída de áudio → seu conector → ID do filtro do driver ligado do outro lado.
    static string? IdDoFiltro(IMMDevice saida)
    {
        var iidTopologia = typeof(IDeviceTopology).GUID;
        if (saida.Activate(ref iidTopologia, 0x17 /* CLSCTX_ALL */, IntPtr.Zero, out var objeto) < 0) return null;
        var topologia = (IDeviceTopology)objeto;
        try
        {
            if (topologia.GetConnector(0, out var conector) < 0) return null;
            try { return conector.GetDeviceIdConnectedTo(out var id) < 0 ? null : id; }
            finally { Marshal.ReleaseComObject(conector); }
        }
        finally { Marshal.ReleaseComObject(topologia); }
    }

    // ---- COM (Core Audio e Kernel Streaming) ----
    static readonly Guid KsPropSetIdBtAudio = new("7FA06C40-B8F6-4C7E-8556-E8C33A12E54D");
    const uint KsPropertyTypeGet = 1;

    [StructLayout(LayoutKind.Sequential)]
    struct KsProperty { public Guid Set; public uint Id; public uint Flags; }

    [StructLayout(LayoutKind.Sequential)]
    struct PropertyKey { public Guid Fmtid; public uint Pid; }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    struct PropVariant
    {
        [FieldOffset(0)] public ushort Vt;
        [FieldOffset(8)] public IntPtr Ponteiro;
    }

    [DllImport("ole32.dll")] static extern int PropVariantClear(ref PropVariant pv);

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumerator { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int fluxo, uint estados, out IMMDeviceCollection colecao);
        [PreserveSig] int GetDefaultAudioEndpoint(int fluxo, int papel, out IMMDevice saida);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice dispositivo);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint total);
        [PreserveSig] int Item(uint indice, out IMMDevice saida);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint contexto, IntPtr parametros, [MarshalAs(UnmanagedType.IUnknown)] out object objeto);
        [PreserveSig] int OpenPropertyStore(uint acesso, out IPropertyStore props);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint total);
        [PreserveSig] int GetAt(uint indice, out PropertyKey chave);
        [PreserveSig] int GetValue(ref PropertyKey chave, out PropVariant valor);
    }

    [ComImport, Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDeviceTopology
    {
        [PreserveSig] int GetConnectorCount(out uint total);
        [PreserveSig] int GetConnector(uint indice, out IConnector conector);
        [PreserveSig] int GetSubunitCount(out uint total);
        [PreserveSig] int GetSubunit(uint indice, [MarshalAs(UnmanagedType.IUnknown)] out object subunidade);
        [PreserveSig] int GetPartById(uint id, [MarshalAs(UnmanagedType.IUnknown)] out object parte);
        [PreserveSig] int GetDeviceId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }

    [ComImport, Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IConnector
    {
        [PreserveSig] int GetConnectorType(out int tipo);
        [PreserveSig] int GetDataFlow(out int fluxo);
        [PreserveSig] int ConnectTo(IConnector outro);
        [PreserveSig] int Disconnect();
        [PreserveSig] int IsConnected([MarshalAs(UnmanagedType.Bool)] out bool conectado);
        [PreserveSig] int GetConnectedTo(out IConnector outro);
        [PreserveSig] int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }

    [ComImport, Guid("28F54685-06FD-11D2-B27A-00A0C9223196"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IKsControl
    {
        [PreserveSig] int KsProperty(ref KsProperty propriedade, uint tamanho, IntPtr dados, uint tamanhoDados, out uint devolvidos);
    }
}
