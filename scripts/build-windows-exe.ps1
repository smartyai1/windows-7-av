param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Project = "src/Av.UI/Av.UI.csproj",
    [string]$OutputDir = "artifacts/win-x64"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK is required. Install .NET 8 SDK and rerun this script."
}

Write-Host "Publishing $Project as a Windows executable..."

dotnet publish $Project `
    -c $Configuration `
    -r $Runtime `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

Write-Host "Build complete. EXE output folder: $OutputDir"
