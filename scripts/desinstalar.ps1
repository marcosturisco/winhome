# Fecha o WinHome, remove o "iniciar com o Windows" e (se confirmado) apaga a configuracao.
$ErrorActionPreference = 'Stop'

Get-Process WinHome -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host 'WinHome fechado (se estava aberto).'

$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (Get-ItemProperty $run -Name WinHome -ErrorAction SilentlyContinue) {
    Remove-ItemProperty $run -Name WinHome
    Write-Host 'Removido: inicializacao com o Windows (HKCU\...\Run\WinHome).'
} else {
    Write-Host 'Inicializacao com o Windows ja estava desligada.'
}

$pasta = Join-Path $env:APPDATA 'WinHome'
if (Test-Path $pasta) {
    $resposta = Read-Host "Apagar a configuracao em $pasta ? (s/N)"
    if ($resposta -eq 's') {
        Remove-Item $pasta -Recurse -Force
        Write-Host 'Configuracao apagada.'
    } else {
        Write-Host 'Configuracao mantida.'
    }
}
