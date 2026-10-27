# Fecha o WinHome, remove o "iniciar com o Windows" e (se confirmado) apaga a configuracao.
$ErrorActionPreference = 'Stop'

# Primeiro pede para sair normalmente (devolve os apps ancorados ao lugar); depois encerra a forca.
$aberto = Get-Process WinHome -ErrorAction SilentlyContinue | Select-Object -First 1
if ($aberto) {
    try { Start-Process $aberto.Path -ArgumentList '--sair' } catch { }
    for ($i = 0; $i -lt 50 -and (Get-Process WinHome -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 100 }
    Get-Process WinHome -ErrorAction SilentlyContinue | Stop-Process -Force
}
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
