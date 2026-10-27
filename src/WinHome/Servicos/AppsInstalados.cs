using System.Runtime.InteropServices;
using WinHome.Configuracao;

namespace WinHome.Servicos;

public sealed record AppInstalado(string Nome, string AppId)
{
    public AppItem ParaItem() => new() { Nome = Nome, App = AppId };
}

// Lista os apps do menu Iniciar (a pasta virtual shell:AppsFolder, a mesma do Get-StartApps).
public static class AppsInstalados
{
    static Task<List<AppInstalado>>? _carga;

    // Carrega uma vez, fora da thread da interface; chamadas seguintes reaproveitam o resultado.
    public static Task<List<AppInstalado>> Obter() => _carga ??= Task.Run(Listar);

    static readonly string[] Ignorar = [".url", ".chm", ".txt", ".pdf", ".htm", ".html", ".rtf"];

    static List<AppInstalado> Listar()
    {
        var lista = new List<AppInstalado>();
        try
        {
            var pasta = FOLDERID_AppsFolder;
            var iidItem = typeof(IShellItem).GUID;
            if (SHGetKnownFolderItem(ref pasta, 0, IntPtr.Zero, ref iidItem, out var raiz) != 0) return lista;

            var bhid = BHID_EnumItems;
            var iidEnum = typeof(IEnumShellItems).GUID;
            if (raiz.BindToHandler(IntPtr.Zero, ref bhid, ref iidEnum, out var ptr) != 0) return lista;
            var itens = (IEnumShellItems)Marshal.GetObjectForIUnknown(ptr);
            Marshal.Release(ptr);

            var lote = new IShellItem[1];
            while (itens.Next(1, lote, out var lidos) == 0 && lidos == 1)
            {
                var nome = Nome(lote[0], SIGDN_NORMALDISPLAY);
                var id = Nome(lote[0], SIGDN_PARENTRELATIVEPARSING);
                Marshal.ReleaseComObject(lote[0]);
                if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(id)) continue;
                if (Ignorar.Any(e => id.EndsWith(e, StringComparison.OrdinalIgnoreCase))) continue;
                if (nome.Contains("Uninstall", StringComparison.OrdinalIgnoreCase) ||
                    nome.Contains("Desinstalar", StringComparison.OrdinalIgnoreCase)) continue;
                lista.Add(new AppInstalado(nome, id));
            }
            Marshal.ReleaseComObject(itens);
            Marshal.ReleaseComObject(raiz);
        }
        catch (COMException)
        {
        }
        return lista.OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    static string? Nome(IShellItem item, uint tipo)
    {
        if (item.GetDisplayName(tipo, out var p) != 0) return null;
        try { return Marshal.PtrToStringUni(p); }
        finally { Marshal.FreeCoTaskMem(p); }
    }

    const uint SIGDN_NORMALDISPLAY = 0;
    const uint SIGDN_PARENTRELATIVEPARSING = 0x80018001;
    static readonly Guid FOLDERID_AppsFolder = new("1e87508d-89c2-42f0-8a7e-645a0f50ca58");
    static readonly Guid BHID_EnumItems = new("94f60519-2850-4924-aa5a-d15e84868039");

    [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem
    {
        [PreserveSig] int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int GetParent(out IShellItem ppsi);
        [PreserveSig] int GetDisplayName(uint sigdnName, out IntPtr ppszName);
        [PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        [PreserveSig] int Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport, Guid("70629033-e363-4a28-a567-0db78006e6d7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IEnumShellItems
    {
        [PreserveSig] int Next(uint celt, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IShellItem[] rgelt, out uint pceltFetched);
        [PreserveSig] int Skip(uint celt);
        [PreserveSig] int Reset();
        [PreserveSig] int Clone(out IEnumShellItems ppenum);
    }

    [DllImport("shell32.dll")]
    static extern int SHGetKnownFolderItem(ref Guid rfid, uint flags, IntPtr hToken, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
}
