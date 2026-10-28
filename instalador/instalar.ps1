# Instala (ou atualiza) o WinHome neste usuario, sem administrador.
# - Copia o programa para %LOCALAPPDATA%\WinHome\app
# - Cria o atalho "WinHome" no menu Iniciar
# - Pergunta: iniciar com o Windows? restaurar um backup?
# - Avisa o que falta para alguns paineis (VLC, adb/scrcpy, IDEs) e abre a Home
# Desfazer: desinstalar.ps1 (no mesmo pacote).
$ErrorActionPreference = 'Stop'

$origem = Join-Path $PSScriptRoot 'app'
$pastaBase = Join-Path $env:LOCALAPPDATA 'WinHome'
$destino = Join-Path $pastaBase 'app'
$exe = Join-Path $destino 'WinHome.exe'

if (-not (Test-Path (Join-Path $origem 'WinHome.exe'))) {
    throw "Pasta 'app' nao encontrada ao lado deste script. Extraia o .zip inteiro antes de instalar."
}

Write-Host ''
Write-Host '=== Instalador do WinHome ===' -ForegroundColor Cyan
Write-Host "Destino: $destino"
Write-Host ''

# 1. Se ja estiver aberto, pede para sair normalmente (devolve apps ancorados); forca depois de 10 s.
if (Get-Process WinHome -ErrorAction SilentlyContinue) {
    Write-Host 'Fechando o WinHome aberto...'
    if (Test-Path $exe) { Start-Process $exe -ArgumentList '--sair' }
    for ($i = 0; $i -lt 100 -and (Get-Process WinHome -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 100 }
    Get-Process WinHome -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

# 2. Copia o programa (espelha a pasta: remove arquivos de versoes antigas).
New-Item -ItemType Directory -Force $destino | Out-Null
robocopy $origem $destino /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar os arquivos (robocopy $LASTEXITCODE)." }
# Arquivos vindos de um .zip baixado ficam marcados como "da internet"; libera os do WinHome.
Get-ChildItem $destino -Recurse -File | Unblock-File
Write-Host 'Programa copiado.' -ForegroundColor Green

# 3. Atalho no menu Iniciar (o da area de trabalho e perguntado abaixo).
function CriarAtalho([string]$pasta) {
    $lnk = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $pasta 'WinHome.lnk'))
    $lnk.TargetPath = $exe
    $lnk.WorkingDirectory = $destino
    $lnk.IconLocation = "$exe,0"
    $lnk.Description = 'WinHome - Home personalizada para o Windows'
    $lnk.Save()
}
CriarAtalho ([Environment]::GetFolderPath('Programs'))
Write-Host 'Atalho criado no menu Iniciar.' -ForegroundColor Green

# 4. Perguntas.
$argumentos = @()
Write-Host ''
$resposta = Read-Host 'Abrir o WinHome ao entrar no Windows? (s/N)'
if ($resposta -match '^[sS]') { $argumentos += '--iniciar-com-windows' }

# Atalho na area de trabalho: reabre a Home depois de "Sair do WinHome". Numa atualizacao, se ja existe, so e refeito.
$areaDeTrabalho = [Environment]::GetFolderPath('Desktop')
if (Test-Path (Join-Path $areaDeTrabalho 'WinHome.lnk')) { CriarAtalho $areaDeTrabalho }
else {
    $resposta = Read-Host 'Criar atalho do WinHome na area de trabalho? (s/N)'
    if ($resposta -match '^[sS]') {
        CriarAtalho $areaDeTrabalho
        Write-Host 'Atalho criado na area de trabalho.' -ForegroundColor Green
    }
}

$resposta = Read-Host 'Restaurar um backup do WinHome (.zip feito em Configuracoes > Backup)? (s/N)'
if ($resposta -match '^[sS]') {
    Add-Type -AssemblyName System.Windows.Forms
    $dialogo = New-Object System.Windows.Forms.OpenFileDialog
    $dialogo.Title = 'Escolha o backup do WinHome'
    $dialogo.Filter = 'Backup do WinHome (*.zip)|*.zip'
    $dialogo.InitialDirectory = [Environment]::GetFolderPath('Desktop')
    if ($dialogo.ShowDialog() -eq 'OK') {
        $argumentos += '--restaurar'
        $argumentos += "`"$($dialogo.FileName)`""
        Write-Host "Backup escolhido: $($dialogo.FileName)"
    }
}

# 5. O que falta neste PC (nada disso impede a Home de funcionar).
Write-Host ''
Write-Host 'Verificando programas usados por alguns paineis:' -ForegroundColor Cyan
function Tem([string]$comando) { return [bool](Get-Command $comando -ErrorAction SilentlyContinue) }
$itens = @(
    @{ Nome = 'VLC 64 bits (painel Videos)'; Ok = (Test-Path "$env:ProgramFiles\VideoLAN\VLC\libvlc.dll"); Dica = 'winget install VideoLAN.VLC' },
    @{ Nome = 'adb (TV e dispositivos)'; Ok = (Tem 'adb'); Dica = 'winget install Google.PlatformTools' },
    @{ Nome = 'scrcpy (espelhar a TV)'; Ok = (Tem 'scrcpy'); Dica = 'winget install Genymobile.scrcpy' },
    @{ Nome = 'Windows Terminal (acoes e projetos)'; Ok = (Tem 'wt'); Dica = 'winget install Microsoft.WindowsTerminal' },
    @{ Nome = 'VS Code (projetos)'; Ok = (Tem 'code'); Dica = 'winget install Microsoft.VisualStudioCode' }
)
foreach ($item in $itens) {
    if ($item.Ok) { Write-Host ("  [ok]    " + $item.Nome) -ForegroundColor Green }
    else { Write-Host ("  [falta] " + $item.Nome + "  ->  " + $item.Dica) -ForegroundColor Yellow }
}
Write-Host '  (IntelliJ e Android Studio sao encontrados sozinhos se estiverem instalados.)'

# 6. Abre a Home.
Write-Host ''
if ($argumentos.Count -gt 0) { Start-Process $exe -ArgumentList $argumentos } else { Start-Process $exe }
Write-Host 'WinHome instalado e aberto. Atalho: Ctrl+Alt+Space (mostrar/esconder).' -ForegroundColor Green
Write-Host 'Se o backup tinha agendas sem o endereco secreto, cole-o em Configuracoes > Agenda.'
