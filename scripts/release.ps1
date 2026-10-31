# Publishes the exe as a GitHub release. Without -Prerelease it is a real release of the csproj Version.
param(
    # A SemVer prerelease label, like ci.42. Makes it a GitHub prerelease, which the app's update check ignores.
    [string]$Prerelease,
    [Parameter(Mandatory)][string]$GitHubToken)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..'
$project = Join-Path $root 'FZScout'
$publish = Join-Path $root 'publish'

$version = dotnet msbuild $project -getProperty:Version
if ($Prerelease) { $version = "$version-$Prerelease" }

dotnet publish $project -c Release -o $publish -p:Version=$version -p:IncludeSourceRevisionInInformationalVersion=false
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
$exe = Join-Path $publish 'FZScout.exe'

$env:GH_TOKEN = $GitHubToken
$flag = if ($Prerelease) { '--prerelease' } else { '--latest' }
gh release create "v$version" $exe --title "v$version" --target (git rev-parse HEAD) --generate-notes $flag
if ($LASTEXITCODE -ne 0) { throw 'release failed' }
