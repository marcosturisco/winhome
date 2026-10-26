# Compila o WinHome (Debug por padrao). Uso: .\scripts\compilar.ps1 [-Release]
param([switch]$Release)

$ErrorActionPreference = 'Stop'
$config = if ($Release) { 'Release' } else { 'Debug' }
$projeto = Join-Path $PSScriptRoot '..\src\WinHome\WinHome.csproj'

dotnet build $projeto -c $config
if ($LASTEXITCODE -ne 0) { throw "Falha na compilacao ($LASTEXITCODE)." }

$exe = Join-Path $PSScriptRoot "..\src\WinHome\bin\$config\net10.0-windows10.0.22621.0\win-x64\WinHome.exe"
Write-Host "OK: $((Resolve-Path $exe).Path)"
