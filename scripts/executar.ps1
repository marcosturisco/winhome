# Compila e abre o WinHome. Se ja estiver aberto, fecha antes para usar a versao nova.
param([switch]$Release)

$ErrorActionPreference = 'Stop'
Get-Process WinHome -ErrorAction SilentlyContinue | Stop-Process -Force
& (Join-Path $PSScriptRoot 'compilar.ps1') -Release:$Release

$config = if ($Release) { 'Release' } else { 'Debug' }
$exe = Join-Path $PSScriptRoot "..\src\WinHome\bin\$config\net10.0-windows10.0.22621.0\win-x64\WinHome.exe"
Start-Process (Resolve-Path $exe).Path
