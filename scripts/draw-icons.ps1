# Renders FZScout's icons from gem.svg and hourglass.svg at every size Windows asks for:
# the emerald gem into FZScout/FZScout.ico,
# the emerald hourglass, for while waiting for a bonus message, into FZScout/FZScoutWaiting.ico,
# a grey gem, for before scouting starts, into FZScout/FZScoutNotScouting.ico,
# a grey gem with a red problem badge, for once the app has stopped on its own, into FZScout/FZScoutStopped.ico,
# and the gem and hourglass with that badge, for after a problem until the next success, into FZScout/FZScoutProblem.ico
# and FZScout/FZScoutWaitingProblem.ico.
# Each size is rendered from the SVG rather than scaled, so the small ones stay crisp.
$ErrorActionPreference = 'Stop'

function WriteIcon([string]$svgName, [string[]]$shownElementIds, [string]$iconName) {
    $sizes = 16, 20, 24, 32, 40, 48, 64, 256
    $workDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory $workDirectory | Out-Null
    try {
        # Show only the <use>s asked for, so one SVG makes several icons.
        $svg = [xml](Get-Content -Raw (Join-Path $PSScriptRoot $svgName))
        foreach ($use in $svg.svg.use) {
            if ($shownElementIds -contains $use.id) { $use.RemoveAttribute('display') } else { $use.SetAttribute('display', 'none') }
        }
        $svgPath = Join-Path $workDirectory 'picture.svg'
        $svg.Save($svgPath)

        & dotnet run (Join-Path $PSScriptRoot 'svg-to-pngs.cs') -- $svgPath $workDirectory @sizes
        if ($LASTEXITCODE -ne 0) { throw "svg-to-pngs failed rendering $iconName" }

        $pngPaths = $sizes | ForEach-Object { Join-Path $workDirectory "$_.png" }
        $iconPath = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\FZScout')).Path $iconName
        & dotnet run (Join-Path $PSScriptRoot 'pngs-to-ico.cs') -- $iconPath @pngPaths
        if ($LASTEXITCODE -ne 0) { throw "pngs-to-ico failed writing $iconName" }
    }
    finally {
        Remove-Item -Recurse -Force $workDirectory
    }
}

WriteIcon 'gem.svg' @('colored-picture') 'FZScout.ico'
WriteIcon 'hourglass.svg' @('colored-picture') 'FZScoutWaiting.ico'
WriteIcon 'gem.svg' @('grey-picture') 'FZScoutNotScouting.ico'
WriteIcon 'gem.svg' @('grey-picture', 'problem-badge') 'FZScoutStopped.ico'
WriteIcon 'gem.svg' @('colored-picture', 'problem-badge') 'FZScoutProblem.ico'
WriteIcon 'hourglass.svg' @('colored-picture', 'problem-badge') 'FZScoutWaitingProblem.ico'
