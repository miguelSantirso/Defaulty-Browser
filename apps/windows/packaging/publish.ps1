# Self-contained single-file publish outline for Defaulty (Windows x64).
# Run from apps/windows/:
#   pwsh packaging/publish.ps1

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts/win-x64"
)

$ErrorActionPreference = "Stop"

$project = "src/Defaulty/Defaulty.csproj"

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $Output

Write-Host "Published to $Output"