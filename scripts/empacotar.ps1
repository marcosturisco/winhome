# Gera o pacote de instalacao: dist\WinHome-<versao>.zip
# Conteudo: app\ (build Release self-contained: .NET e Windows App SDK inclusos), instalar/desinstalar (.ps1 e .cmd)
# e LEIA-ME.txt. Uso: .\scripts\empacotar.ps1
$ErrorActionPreference = 'Stop'

$raiz = Resolve-Path (Join-Path $PSScriptRoot '..')
$projeto = Join-Path $raiz 'src\WinHome\WinHome.csproj'
$versao = ([xml](Get-Content $projeto)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$dist = Join-Path $raiz 'dist'
$pacote = Join-Path $dist 'pacote'
$zip = Join-Path $dist "WinHome-$versao.zip"

if (Test-Path $pacote) { Remove-Item $pacote -Recurse -Force }
New-Item -ItemType Directory -Force $pacote | Out-Null

Write-Host "Publicando WinHome $versao (Release, self-contained)..."
dotnet publish $projeto -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -o (Join-Path $pacote 'app')
if ($LASTEXITCODE -ne 0) { throw "Falha na publicacao ($LASTEXITCODE)." }

Copy-Item (Join-Path $raiz 'instalador\*') $pacote
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $pacote '*') -DestinationPath $zip -CompressionLevel Optimal

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "OK: $zip ($mb MB)"
