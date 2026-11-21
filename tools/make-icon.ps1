# Draws the app icon (a crescent moon on a night-blue squircle) into a
# multi-size .ico. Every size is drawn at 4x and downsampled, and every edge
# is anti-aliased, so the icon stays crisp at any desktop scale.
# Run once; the output is committed.
param([string]$Out = "$PSScriptRoot\..\src\Dusk\Assets\dusk.ico")

Add-Type -AssemblyName System.Drawing

function New-Frame([int]$size) {
    $s = $size * 4
    $big = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded square
    $m = [single]($s * 0.04); $w = [single]($s - 2 * $m); $d = [single]($s * 0.45)
    $sq = New-Object System.Drawing.Drawing2D.GraphicsPath
    $sq.AddArc($m, $m, $d, $d, 180, 90)
    $sq.AddArc($m + $w - $d, $m, $d, $d, 270, 90)
    $sq.AddArc($m + $w - $d, $m + $w - $d, $d, $d, 0, 90)
    $sq.AddArc($m, $m + $w - $d, $d, $d, 90, 90)
    $sq.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF 0, 0, $s, $s
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect,
        ([System.Drawing.Color]::FromArgb(255, 78, 88, 204)),
        ([System.Drawing.Color]::FromArgb(255, 22, 27, 72)), 90
    $g.FillPath($bg, $sq)

    # Crescent: a white disc, then the same background painted over an offset
    # disc. Both are anti-aliased paths, unlike a Region, so edges are smooth.
    $c = [single]($s * 0.5); $R = [single]($s * 0.27); $o = [single]($R * 0.55)
    $g.FillEllipse([System.Drawing.Brushes]::White, $c - $R, $c - $R, 2 * $R, 2 * $R)
    $g.SetClip($sq)
    $g.FillEllipse($bg, $c - $R + $o, $c - $R - $o * 0.55, 2 * $R, 2 * $R)
    $g.Dispose()

    # Downsample 4x with a high-quality filter.
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g2 = [System.Drawing.Graphics]::FromImage($bmp)
    $g2.InterpolationMode = 'HighQualityBicubic'
    $g2.PixelOffsetMode = 'HighQuality'
    $g2.CompositingQuality = 'HighQuality'
    $g2.DrawImage($big, 0, 0, $size, $size)
    $g2.Dispose(); $big.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

# All sizes Windows asks for at 100%–300% scaling (taskbar, tray, desktop, Explorer).
$sizes = 16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 128, 256
$frames = $sizes | ForEach-Object { ,(New-Frame $_) }
New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
$fs = [System.IO.File]::Create($Out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $len = $frames[$i].Length
    $b = if ($sz -ge 256) { 0 } else { $sz }
    $bw.Write([byte]$b); $bw.Write([byte]$b); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$len); $bw.Write([uint32]$offset)
    $offset += $len
}
foreach ($f in $frames) { $bw.Write($f) }
$bw.Close()
Write-Output "ok $Out ($($sizes.Count) boyut)"
