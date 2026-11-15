<#
.SYNOPSIS
    Publishes Kodi Reader to %LOCALAPPDATA%\Programs\KodiReader and starts it.
.DESCRIPTION
    Uses the "Local" publish profile of KodiReader.Tray. Any running Kodi Reader is stopped first,
    because it locks its files and its single-instance check would block the new version.
    Note: appsettings.json in the target folder is overwritten by the one from the project.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\publish.ps1
#>
param(
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "KodiReader.Tray\KodiReader.Tray.csproj"
$publishDir = Join-Path $env:LOCALAPPDATA "Programs\KodiReader"
$exe = Join-Path $publishDir "KodiReader.exe"

Get-Process -Name KodiReader -ErrorAction SilentlyContinue | Stop-Process -Force

dotnet publish $project -c Release -p:PublishProfile=Local
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Published to $publishDir"

if (-not $NoStart) {
    Start-Process -FilePath $exe
    Write-Host "Kodi Reader started. Turn on 'Start with Windows' in its tray menu (once)."
}
