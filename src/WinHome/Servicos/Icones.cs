using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinHome.Configuracao;

namespace WinHome.Servicos;

// Ícones vindos do próprio shell do Windows (mesmos do menu Iniciar), com cache em memória.
public static class Icones
{
    static readonly Dictionary<string, ImageSource?> Cache = [];

    public static ImageSource? De(AppItem item, int tamanho)
    {
        var chave = $"{item.Icone}|{item.App}|{item.Caminho}|{tamanho}";
        if (Cache.TryGetValue(chave, out var pronto)) return pronto;

        ImageSource? imagem = null;
        if (!string.IsNullOrWhiteSpace(item.Icone))
        {
            var arquivo = Environment.ExpandEnvironmentVariables(item.Icone);
            if (File.Exists(arquivo)) imagem = new BitmapImage(new Uri(arquivo));
        }
        else if (!string.IsNullOrWhiteSpace(item.App))
        {
            imagem = DoShell($"shell:AppsFolder\\{item.App}", tamanho);
        }
        else if (!string.IsNullOrWhiteSpace(item.Caminho))
        {
            imagem = DoShell(Environment.ExpandEnvironmentVariables(item.Caminho), tamanho);
        }

        Cache[chave] = imagem;
        return imagem;
    }

    static WriteableBitmap? DoShell(string nome, int tamanho)
    {
        var iid = typeof(IShellItemImageFactory).GUID;
        if (SHCreateItemFromParsingName(nome, IntPtr.Zero, ref iid, out var fabrica) != 0) return null;
        try
        {
            if (fabrica.GetImage(new SIZE(tamanho, tamanho), SIIGBF_ICONONLY | SIIGBF_BIGGERSIZEOK, out var hbm) != 0)
                return null;
            try { return ParaBitmap(hbm); }
            finally { DeleteObject(hbm); }
        }
        finally
        {
            Marshal.ReleaseComObject(fabrica);
        }
    }

    static WriteableBitmap? ParaBitmap(IntPtr hbm)
    {
        if (GetObject(hbm, Marshal.SizeOf<BITMAP>(), out var bmp) == 0) return null;
        int w = bmp.bmWidth, h = Math.Abs(bmp.bmHeight);
        var cabecalho = new BITMAPINFOHEADER
        {
            biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = w,
            biHeight = -h, // de cima para baixo
            biPlanes = 1,
            biBitCount = 32,
        };
        var pixels = new byte[w * h * 4];
        var dc = GetDC(IntPtr.Zero);
        try
        {
            if (GetDIBits(dc, hbm, 0, (uint)h, pixels, ref cabecalho, 0) == 0) return null;
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, dc);
        }

        // Ícones antigos sem canal alfa viriam totalmente transparentes.
        bool temAlfa = false;
        for (int i = 3; i < pixels.Length && !temAlfa; i += 4) temAlfa = pixels[i] != 0;
        if (!temAlfa)
            for (int i = 3; i < pixels.Length; i += 4) pixels[i] = 255;

        var bitmap = new WriteableBitmap(w, h);
        using (var fluxo = bitmap.PixelBuffer.AsStream()) fluxo.Write(pixels);
        bitmap.Invalidate();
        return bitmap;
    }

    const int SIIGBF_BIGGERSIZEOK = 0x1;
    const int SIIGBF_ICONONLY = 0x4;

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItemImageFactory
    {
        [PreserveSig] int GetImage(SIZE size, int flags, out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    readonly struct SIZE(int cx, int cy)
    {
        readonly int cx = cx, cy = cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAP
    {
        public int bmType, bmWidth, bmHeight, bmWidthBytes;
        public ushort bmPlanes, bmBitsPixel;
        public IntPtr bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")] static extern int GetObject(IntPtr h, int c, out BITMAP pv);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr h);
    [DllImport("gdi32.dll")]
    static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, byte[] bits,
        ref BITMAPINFOHEADER bmi, uint usage);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
}
