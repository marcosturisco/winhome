# Gera src\WinHome\Assets\app.ico (16 a 256 px, PNG dentro do ICO) e uma previa em PNG.
# Desenho: quadrado arredondado em degrade azul -> violeta com uma casa branca.
param([string]$Previa = '')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function Desenhar([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Fundo arredondado
    $r = [single]($s * 0.22)
    $m = [single]([Math]::Max(0.5, $s * 0.02))
    $w = [single]($s - 2 * $m)
    $fundo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $fundo.AddArc($m, $m, 2 * $r, 2 * $r, 180, 90)
    $fundo.AddArc($m + $w - 2 * $r, $m, 2 * $r, 2 * $r, 270, 90)
    $fundo.AddArc($m + $w - 2 * $r, $m + $w - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $fundo.AddArc($m, $m + $w - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $fundo.CloseFigure()
    $c1 = [System.Drawing.Color]::FromArgb(255, 0x4F, 0x8C, 0xFF)
    $c2 = [System.Drawing.Color]::FromArgb(255, 0x7C, 0x5C, 0xFF)
    $degrade = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF $s, $s), $c1, $c2
    $g.FillPath($degrade, $fundo)

    # Casa
    $p = {
        param($x, $y)
        New-Object System.Drawing.PointF ([single]($x * $s)), ([single]($y * $s))
    }
    $casa = [System.Drawing.PointF[]]@(
        (& $p 0.50 0.22), (& $p 0.81 0.50), (& $p 0.72 0.50), (& $p 0.72 0.78),
        (& $p 0.28 0.78), (& $p 0.28 0.50), (& $p 0.19 0.50))
    $g.FillPolygon([System.Drawing.Brushes]::White, $casa)

    # Porta (na cor do fundo)
    $porta = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0x66, 0x74, 0xFF))
    $g.FillRectangle($porta, [single](0.43 * $s), [single](0.58 * $s), [single](0.14 * $s), [single](0.20 * $s))

    $g.Dispose()
    return $bmp
}

$tamanhos = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = foreach ($t in $tamanhos) {
    $bmp = Desenhar $t
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($Previa -and $t -eq 256) { $bmp.Save($Previa, [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    , $ms.ToArray()
}

# Formato ICO: cabecalho (6 bytes) + uma entrada de 16 bytes por tamanho + os PNGs
$saida = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $saida
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$tamanhos.Count)
$offset = 6 + 16 * $tamanhos.Count
for ($i = 0; $i -lt $tamanhos.Count; $i++) {
    $t = $tamanhos[$i]
    $lado = if ($t -ge 256) { 0 } else { $t }
    $bw.Write([byte]$lado); $bw.Write([byte]$lado); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($png in $pngs) { $bw.Write($png) }
$bw.Flush()

$destino = Join-Path $PSScriptRoot '..\src\WinHome\Assets\app.ico'
[System.IO.File]::WriteAllBytes($destino, $saida.ToArray())
Write-Host "OK: $((Resolve-Path $destino).Path) ($($saida.Length) bytes)"
