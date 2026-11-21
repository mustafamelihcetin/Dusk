# Draws the app icon (a crescent moon on a night-blue squircle) into a
# multi-size .ico. Run once; the output is committed.
param([string]$Out = "$PSScriptRoot\..\src\Dusk\Assets\dusk.ico")

Add-Type -AssemblyName System.Drawing

function New-Frame([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded square
    $r = [single]($s * 0.225); $d = $r * 2; $m = [single]($s * 0.04); $w = [single]($s - 2 * $m)
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($m, $m, $d, $d, 180, 90)
    $p.AddArc($m + $w - $d, $m, $d, $d, 270, 90)
    $p.AddArc($m + $w - $d, $m + $w - $d, $d, $d, 0, 90)
    $p.AddArc($m, $m + $w - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    $top = [System.Drawing.Color]::FromArgb(255, 74, 84, 196)
    $bot = [System.Drawing.Color]::FromArgb(255, 22, 27, 72)
    $rect = New-Object System.Drawing.RectangleF 0, 0, $s, $s
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $top, $bot, 90
    $g.FillPath($brush, $p)

    # Crescent: a disc minus an offset disc
    $c = [single]($s * 0.5); $R = [single]($s * 0.27)
    $moon = New-Object System.Drawing.Drawing2D.GraphicsPath
    $moon.AddEllipse($c - $R, $c - $R, 2 * $R, 2 * $R)
    $cut = New-Object System.Drawing.Drawing2D.GraphicsPath
    $o = [single]($R * 0.55)
    $cut.AddEllipse($c - $R + $o, $c - $R - $o * 0.55, 2 * $R, 2 * $R)
    $region = New-Object System.Drawing.Region $moon
    $region.Exclude($cut)
    $g.FillRegion([System.Drawing.Brushes]::White, $region)

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

$sizes = 16, 20, 24, 32, 48, 64, 256
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
Write-Output "ok $Out"
