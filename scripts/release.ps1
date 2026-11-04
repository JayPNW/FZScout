# Publishes a Velopack release to GitHub. Without -Prerelease it is a real release of the csproj Version.
param(
    # A SemVer prerelease label, like ci.42. Makes it a GitHub prerelease, which the app's update check ignores.
    [string]$Prerelease,
    [Parameter(Mandatory)][string]$GitHubToken)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..'
$project = Join-Path $root 'FZScout'
$publish = Join-Path $root 'publish'
$releases = Join-Path $root 'releases'
$repoUrl = 'https://github.com/JayPNW/FZScout'

$version = dotnet msbuild $project -getProperty:Version
if ($Prerelease) { $version = "$version-$Prerelease" }

# Velopack requires a 3-part SemVer version, unlike the project's plain release number.
if ($version -notmatch '^(?<release>\d+)(-(?<prerelease>.+))?$') { throw "Unexpected version format: $version" }
$packVersion = "$($Matches.release).0.0"
if ($Matches.prerelease) { $packVersion = "$packVersion-$($Matches.prerelease)" }

dotnet publish $project -c Release -o $publish -p:Version=$version -p:IncludeSourceRevisionInInformationalVersion=false
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }

# No deltas: there is nothing to diff against on the first Velopack release, and this app is small enough that full packages are fine.
vpk pack --packId FZScout --packVersion $packVersion --packDir $publish --mainExe FZScout.exe --icon (Join-Path $project 'FZScout.ico') --runtime win-x64 --outputDir $releases --delta None
if ($LASTEXITCODE -ne 0) { throw 'pack failed' }

$uploadArgs = @(
    'upload', 'github',
    '--repoUrl', $repoUrl,
    '--token', $GitHubToken,
    '--outputDir', $releases,
    '--publish',
    '--releaseName', "v$version",
    '--tag', "v$version",
    '--targetCommitish', (git rev-parse HEAD))
if ($Prerelease) { $uploadArgs += '--pre' }

vpk @uploadArgs
if ($LASTEXITCODE -ne 0) { throw 'release failed' }
