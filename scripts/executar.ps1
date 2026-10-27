# Compila e abre o WinHome. Se ja estiver aberto, fecha antes para usar a versao nova.
param([switch]$Release)

$ErrorActionPreference = 'Stop'
$config = if ($Release) { 'Release' } else { 'Debug' }
$exe = Join-Path $PSScriptRoot "..\src\WinHome\bin\$config\net10.0-windows10.0.22621.0\win-x64\WinHome.exe"

# Pede para a Home sair normalmente (devolve os apps ancorados ao lugar); so encerra a forca se nao sair em 10 s.
if (Get-Process WinHome -ErrorAction SilentlyContinue) {
    if (Test-Path $exe) { Start-Process $exe -ArgumentList '--sair' }
    for ($i = 0; $i -lt 100 -and (Get-Process WinHome -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 100 }
    Get-Process WinHome -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

& (Join-Path $PSScriptRoot 'compilar.ps1') -Release:$Release
Start-Process (Resolve-Path $exe).Path
