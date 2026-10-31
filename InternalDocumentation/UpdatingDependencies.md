# Updating Dependencies

| Dependency | Where it's pinned | Who updates it |
| --- | --- | --- |
| NuGet packages | [Directory.Packages.props](/Directory.Packages.props) | Dependabot |
| GitHub Actions | [.github/workflows/](/.github/workflows/) | Dependabot |
| .NET tools | [dotnet-tools.json](/dotnet-tools.json) | Us |
| .NET SDK | [global.json](/global.json) | Us |

## What Dependabot does

[dependabot.yml](/.github/dependabot.yml) keeps NuGet packages and GitHub Actions up to date.

## What we do

### .NET SDK

[global.json](/global.json) pins an exact SDK version. We always want to use the latest LTS.
