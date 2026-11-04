# Draws FZScout's icon, an emerald gem, into FZScout/FZScout.ico at every size Windows asks for.
# Each size is drawn separately rather than scaled, so the small ones stay crisp.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot '..\FZScout\FZScout.ico'

# On a 32-unit grid: flat table on top, crown down to the girdle, pavilion down to a point.
$T1=@(10,4); $T2=@(22,4); $G0=@(2,12); $G1=@(11,12); $G2=@(21,12); $G3=@(30,12); $P=@(16,29)
$facets = @(
    @{ Points = @($T1,$G0,$G1);     Color = '#76E3A6' }  # crown left
    @{ Points = @($T1,$T2,$G2,$G1); Color = '#C8FADF' }  # table
    @{ Points = @($T2,$G3,$G2);     Color = '#36C47A' }  # crown right
    @{ Points = @($G0,$G1,$P);      Color = '#25A363' }  # pavilion left
    @{ Points = @($G1,$G2,$P);      Color = '#17804B' }  # pavilion middle
    @{ Points = @($G2,$G3,$P);      Color = '#0E5F37' }) # pavilion right
$outline = @($T1,$T2,$G3,$P,$G0)
$outlineColor = '#05301B'

function ToPoints($points, [double]$scale) {
    # The leading comma keeps PowerShell from unrolling the array into separate outputs.
    ,[System.Drawing.PointF[]]($points | ForEach-Object { New-Object System.Drawing.PointF ($_[0] * $scale), ($_[1] * $scale) })
}

function Draw([int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $scale = $size / 32.0
    foreach ($facet in $facets) {
        $g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($facet.Color))), (ToPoints $facet.Points $scale))
    }
    $pen = New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($outlineColor)), ([Math]::Max(1.0, 1.6 * $scale))
    $pen.LineJoin = 'Round'
    $g.DrawPolygon($pen, (ToPoints $outline $scale))
    $g.Dispose()
    ,$bitmap
}

# Small sizes as 32-bit BMPs, which everything reads; 256 as PNG, which is what Windows expects at that size.
function ToBmpEntry($bitmap) {
    $size = $bitmap.Width
    $stream = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter $stream
    $maskRowBytes = [int]([Math]::Ceiling($size / 32.0) * 4)
    $w.Write([int]40); $w.Write([int]$size); $w.Write([int]($size * 2)); $w.Write([int16]1); $w.Write([int16]32)
    $w.Write([int]0); $w.Write([int]($size * $size * 4 + $maskRowBytes * $size)); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0); $w.Write([int]0)
    for ($y = $size - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $size; $x++) {
            $c = $bitmap.GetPixel($x, $y)
            $w.Write([byte]$c.B); $w.Write([byte]$c.G); $w.Write([byte]$c.R); $w.Write([byte]$c.A)
        }
    }
    $w.Write((New-Object byte[] ($maskRowBytes * $size)))
    $w.Flush()
    $stream.ToArray()
}

function ToPngEntry($bitmap) {
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $stream.ToArray()
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$entries = foreach ($size in $sizes) {
    $bitmap = Draw $size
    if ($size -eq 256) { $data = ToPngEntry $bitmap } else { $data = ToBmpEntry $bitmap }
    @{ Size = $size; Data = $data }
}

$file = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $file
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$entries.Count)
$offset = 6 + 16 * $entries.Count
foreach ($entry in $entries) {
    $dimension = $entry.Size % 256
    $w.Write([byte]$dimension); $w.Write([byte]$dimension); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]$entry.Data.Length); $w.Write([int]$offset)
    $offset += $entry.Data.Length
}
foreach ($entry in $entries) { $w.Write([byte[]]$entry.Data) }
$w.Flush()
[System.IO.File]::WriteAllBytes((Resolve-Path (Split-Path $out)).Path + '\' + (Split-Path $out -Leaf), $file.ToArray())
