@echo off
setlocal

set CONFIGURATION=Release
set RUNTIME=win-x64
set PROJECT=src\Av.UI\Av.UI.csproj
set OUTPUT=artifacts\win-x64

where dotnet >nul 2>nul
if errorlevel 1 (
  echo dotnet SDK not found. Install .NET 8 SDK first.
  exit /b 1
)

dotnet publish %PROJECT% ^
  -c %CONFIGURATION% ^
  -r %RUNTIME% ^
  -p:PublishSingleFile=true ^
  -p:SelfContained=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o %OUTPUT%

if errorlevel 1 exit /b 1

echo Build complete. EXE output folder: %OUTPUT%
endlocal
