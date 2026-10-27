# Remove o WinHome instalado por instalar.ps1 (sem administrador).
# Fecha a Home (saida normal: devolve apps ancorados), tira o "iniciar com o Windows" e o atalho,
# apaga o programa e, se confirmado, a configuracao.
$ErrorActionPreference = 'Stop'

$pastaBase = Join-Path $env:LOCALAPPDATA 'WinHome'
$exe = Join-Path $pastaBase 'app\WinHome.exe'

Write-Host ''
Write-Host '=== Desinstalar o WinHome ===' -ForegroundColor Cyan

if (Get-Process WinHome -ErrorAction SilentlyContinue) {
    if (Test-Path $exe) { Start-Process $exe -ArgumentList '--sair' }
    for ($i = 0; $i -lt 50 -and (Get-Process WinHome -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 100 }
    Get-Process WinHome -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host 'WinHome fechado.'
}

$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (Get-ItemProperty $run -Name WinHome -ErrorAction SilentlyContinue) {
    Remove-ItemProperty $run -Name WinHome
    Write-Host 'Removido: iniciar com o Windows.'
}

$atalho = Join-Path ([Environment]::GetFolderPath('Programs')) 'WinHome.lnk'
if (Test-Path $atalho) { Remove-Item $atalho -Force; Write-Host 'Removido: atalho do menu Iniciar.' }

# Programa e dados do navegador interno (YouTube) em %LOCALAPPDATA%\WinHome.
if (Test-Path $pastaBase) {
    Start-Sleep -Milliseconds 500
    Remove-Item $pastaBase -Recurse -Force
    Write-Host "Removido: $pastaBase"
}

$config = Join-Path $env:APPDATA 'WinHome'
if (Test-Path $config) {
    $resposta = Read-Host "Apagar tambem a configuracao, tarefas e backups em $config ? (s/N)"
    if ($resposta -match '^[sS]') { Remove-Item $config -Recurse -Force; Write-Host 'Configuracao apagada.' }
    else { Write-Host 'Configuracao mantida (uma nova instalacao a reaproveita).' }
}
Write-Host 'Pronto.' -ForegroundColor Green
