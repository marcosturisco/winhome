# Publica uma versao nova do WinHome no GitHub, de ponta a ponta:
#   1. confere: branch main, sem mudancas pendentes, versao ainda nao publicada, GitHub CLI logado
#   2. muda <Version> no WinHome.csproj e faz o commit "Versao X"
#   3. gera o pacote (empacotar.ps1) -> dist\WinHome-X.zip
#   4. cria a tag vX, envia main + tag (chave SSH passada so no comando)
#   5. cria o Release vX com o .zip anexado e as notas (docs\notas-release.md + commits desde a versao anterior)
# Uso: .\scripts\publicar.ps1 -Versao 1.0.1
param([Parameter(Mandatory = $true)][string]$Versao)
$ErrorActionPreference = 'Stop'

if ($Versao -notmatch '^\d+\.\d+\.\d+$') { throw "Versao '$Versao' invalida. Use o formato 1.2.3." }
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projeto = Join-Path $raiz 'src\WinHome\WinHome.csproj'
$tag = "v$Versao"
$chave = (Join-Path $env:USERPROFILE '.ssh\id_ed25519') -replace '\\', '/'
$ssh = "core.sshCommand='C:/Program Files/Git/usr/bin/ssh.exe' -F none -i '$chave' -o IdentitiesOnly=yes"
Push-Location $raiz
try {
    # 1. Conferencias (nada e alterado se alguma falhar).
    $gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
    if (-not $gh) { $gh = Get-ChildItem (Join-Path $env:LOCALAPPDATA 'Programs\GitHubCLI') -Recurse -Filter gh.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
    if (-not $gh) { throw 'GitHub CLI (gh) nao encontrado. Veja docs\reversao.md (instalado em %LOCALAPPDATA%\Programs\GitHubCLI).' }
    & $gh auth status *> $null
    if ($LASTEXITCODE -ne 0) { throw "GitHub CLI sem login. Rode uma vez: & '$gh' auth login" }
    if ((git branch --show-current) -ne 'main') { throw 'Publique a partir da branch main.' }
    if (git status --porcelain) { throw 'Ha mudancas nao commitadas. Faca o commit (ou descarte) antes de publicar.' }
    if (git tag -l $tag) { throw "A tag $tag ja existe." }
    git -c $ssh fetch origin main --quiet
    if ((git rev-parse HEAD) -ne (git rev-parse origin/main)) { throw 'A main local nao esta igual a do GitHub. Envie (push) ou atualize antes.' }
    $anterior = git describe --tags --abbrev=0 2>$null

    # 2. Versao no projeto (grava em UTF-8 sem BOM: o csproj tem comentarios com acento).
    $texto = [IO.File]::ReadAllText($projeto)
    $novo = [regex]::Replace($texto, '<Version>[^<]*</Version>', "<Version>$Versao</Version>")
    if ($novo -ne $texto) {
        [IO.File]::WriteAllText($projeto, $novo, (New-Object System.Text.UTF8Encoding $false))
        git add $projeto
        git commit -q -m "Versao $Versao"
    }

    # 3. Pacote.
    & (Join-Path $PSScriptRoot 'empacotar.ps1')
    $zip = Join-Path $raiz "dist\WinHome-$Versao.zip"
    if (-not (Test-Path $zip)) { throw "Pacote nao gerado: $zip" }

    # 4. Tag e envio.
    git tag -a $tag -m "WinHome $Versao"
    git -c $ssh push origin main $tag
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao enviar para o GitHub.' }

    # 5. Release com o .zip e as notas.
    $mudancas = if ($anterior) { (git log "$anterior..HEAD" --no-merges --pretty=format:'- %s') -join "`n" } else { '- Primeira versao.' }
    $modelo = [IO.File]::ReadAllText((Join-Path $raiz 'docs\notas-release.md'), [Text.Encoding]::UTF8)
    $notas = $modelo.Replace('{versao}', $Versao).Replace('{anterior}', $(if ($anterior) { $anterior } else { 'o inicio' })).Replace('{mudancas}', $mudancas)
    $arquivoNotas = Join-Path $env:TEMP "winhome-notas-$Versao.md"
    [IO.File]::WriteAllText($arquivoNotas, $notas, (New-Object System.Text.UTF8Encoding $false))
    & $gh release create $tag $zip --title "WinHome $Versao" --notes-file $arquivoNotas --latest
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar o Release (a tag ja foi enviada; crie o Release com: gh release create).' }
    Remove-Item $arquivoNotas -Force
    Write-Host "Publicado: https://github.com/marcosturisco/winhome/releases/tag/$tag" -ForegroundColor Green
}
finally { Pop-Location }
