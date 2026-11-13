# Draws FZScout's icons at every size Windows asks for: an emerald gem into FZScout/FZScout.ico,
# an emerald hourglass, for while waiting for a bonus message, into FZScout/FZScoutWaiting.ico,
# a grey gem, for before scouting starts, into FZScout/FZScoutNotScouting.ico,
# a grey gem with a red problem badge, for once the app has stopped on its own, into FZScout/FZScoutStopped.ico,
# and the gem and hourglass with that badge, for after a problem until the next success, into FZScout/FZScoutProblem.ico
# and FZScout/FZScoutWaitingProblem.ico.
# Each size is drawn separately rather than scaled, so the small ones stay crisp.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# On a 32-unit grid: flat table on top, crown down to the girdle, pavilion down to a point.
$T1=@(10,4); $T2=@(22,4); $G0=@(2,12); $G1=@(11,12); $G2=@(21,12); $G3=@(30,12); $P=@(16,29)
$gem = @{
    Facets = @(
        @{ Points = @($T1,$G0,$G1);     Color = '#76E3A6' }  # crown left
        @{ Points = @($T1,$T2,$G2,$G1); Color = '#C8FADF' }  # table
        @{ Points = @($T2,$G3,$G2);     Color = '#36C47A' }  # crown right
        @{ Points = @($G0,$G1,$P);      Color = '#25A363' }  # pavilion left
        @{ Points = @($G1,$G2,$P);      Color = '#17804B' }  # pavilion middle
        @{ Points = @($G2,$G3,$P);      Color = '#0E5F37' }) # pavilion right
    Outlines = @(,@($T1,$T2,$G3,$P,$G0))
    OutlineColor = '#05301B'
}

# On the same grid and in the gem's colors: a cap at each end, and two glass bulbs meeting at the waist, lit from the upper left.
$CapTopLeft=@(6,2); $CapTopRight=@(26,2); $BulbTopLeft=@(6,6); $BulbTopRight=@(26,6); $TopMiddle=@(16,6)
$Waist=@(16,16)
$BulbBottomLeft=@(6,26); $BulbBottomRight=@(26,26); $BottomMiddle=@(16,26); $CapBottomLeft=@(6,30); $CapBottomRight=@(26,30)
$hourglass = @{
    Facets = @(
        @{ Points = @($CapTopLeft,$CapTopRight,$BulbTopRight,$BulbTopLeft);             Color = '#36C47A' }  # top cap
        @{ Points = @($BulbTopLeft,$TopMiddle,$Waist);                                   Color = '#C8FADF' }  # top bulb left
        @{ Points = @($TopMiddle,$BulbTopRight,$Waist);                                  Color = '#76E3A6' }  # top bulb right
        @{ Points = @($Waist,$BottomMiddle,$BulbBottomLeft);                             Color = '#25A363' }  # bottom bulb left
        @{ Points = @($Waist,$BulbBottomRight,$BottomMiddle);                            Color = '#17804B' }  # bottom bulb right
        @{ Points = @($BulbBottomLeft,$BulbBottomRight,$CapBottomRight,$CapBottomLeft); Color = '#0E5F37' }) # bottom cap
    Outlines = @(
        @($CapTopLeft,$CapTopRight,$BulbTopRight,$BulbTopLeft)
        @($BulbTopLeft,$BulbTopRight,$Waist)
        @($Waist,$BulbBottomRight,$BulbBottomLeft)
        @($BulbBottomLeft,$BulbBottomRight,$CapBottomRight,$CapBottomLeft))
    OutlineColor = '#05301B'
}

function Grey([string]$color) {
    $c = [System.Drawing.ColorTranslator]::FromHtml($color)
    $brightness = [int](0.299 * $c.R + 0.587 * $c.G + 0.114 * $c.B)
    '#{0:X2}{0:X2}{0:X2}' -f $brightness
}

# The same shapes, each color as bright as before, but grey.
function Greyed($picture) {
    @{
        Facets = @($picture.Facets | ForEach-Object { @{ Points = $_.Points; Color = (Grey $_.Color) } })
        Outlines = $picture.Outlines
        OutlineColor = (Grey $picture.OutlineColor)
        Overlays = $picture.Overlays
    }
}

function Circle([double]$centerX, [double]$centerY, [double]$radius) {
    $corners = 24
    ,@(0..($corners - 1) | ForEach-Object { $angle = 2 * [Math]::PI * $_ / $corners; ,@(($centerX + $radius * [Math]::Cos($angle)), ($centerY + $radius * [Math]::Sin($angle))) })
}

# A red circle with a white exclamation mark in the bottom right corner, drawn over the picture and its outlines.
function WithProblemBadge($picture) {
    @{
        Facets = $picture.Facets
        Outlines = $picture.Outlines
        OutlineColor = $picture.OutlineColor
        Overlays = @(
            @{ Points = (Circle 24 24 8);   Color = '#3A0505' }  # rim
            @{ Points = (Circle 24 24 6.5); Color = '#E02424' }  # badge
            @{ Points = @(@(22.75,19), @(25.25,19), @(25.25,25), @(22.75,25)); Color = '#FFFFFF' }   # exclamation mark
            @{ Points = @(@(22.75,26.5), @(25.25,26.5), @(25.25,29), @(22.75,29)); Color = '#FFFFFF' }) # its dot
    }
}

function ToPoints($points, [double]$scale) {
    # The leading comma keeps PowerShell from unrolling the array into separate outputs.
    ,[System.Drawing.PointF[]]($points | ForEach-Object { New-Object System.Drawing.PointF ($_[0] * $scale), ($_[1] * $scale) })
}

function Draw($picture, [int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    # Otherwise the PNG records the display's DPI, which differs from one process to the next, so the same picture gets different bytes.
    $bitmap.SetResolution(96, 96)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $scale = $size / 32.0
    foreach ($facet in $picture.Facets) {
        $g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($facet.Color))), (ToPoints $facet.Points $scale))
    }
    $pen = New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($picture.OutlineColor)), ([Math]::Max(1.0, 1.6 * $scale))
    $pen.LineJoin = 'Round'
    foreach ($outline in $picture.Outlines) {
        $g.DrawPolygon($pen, (ToPoints $outline $scale))
    }
    foreach ($overlay in $picture.Overlays) {
        $g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($overlay.Color))), (ToPoints $overlay.Points $scale))
    }
    $g.Dispose()
    ,$bitmap
}

# ImageMagick stores the small sizes as 32-bit BMPs, which everything reads, and 256 as PNG, which is what Windows expects at that size.
function WriteIcon($picture, [string]$fileName) {
    $sizes = 16, 20, 24, 32, 40, 48, 64, 256
    $workDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory $workDirectory | Out-Null
    try {
        $pngPaths = foreach ($size in $sizes) {
            $pngPath = Join-Path $workDirectory "$size.png"
            $bitmap = Draw $picture $size
            $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
            $bitmap.Dispose()
            $pngPath
        }
        $iconPath = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\FZScout')).Path $fileName
        & dotnet run (Join-Path $PSScriptRoot 'pngs-to-ico.cs') -- $iconPath @pngPaths
        if ($LASTEXITCODE -ne 0) { throw "pngs-to-ico failed writing $fileName" }
    }
    finally {
        Remove-Item -Recurse -Force $workDirectory
    }
}

WriteIcon $gem 'FZScout.ico'
WriteIcon $hourglass 'FZScoutWaiting.ico'
WriteIcon (Greyed $gem) 'FZScoutNotScouting.ico'
WriteIcon (WithProblemBadge (Greyed $gem)) 'FZScoutStopped.ico'
WriteIcon (WithProblemBadge $gem) 'FZScoutProblem.ico'
WriteIcon (WithProblemBadge $hourglass) 'FZScoutWaitingProblem.ico'
