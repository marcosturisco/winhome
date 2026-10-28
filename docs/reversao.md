# Registro de alterações no PC e como desfazer

| Data | Alteração | Como desfazer |
|---|---|---|
| 26/10/2026 | Instalado .NET 10 SDK (10.0.403) via winget, para compilar o projeto | `winget uninstall --id Microsoft.DotNet.SDK.10 --exact` |
| 27/10/2026 | GitHub CLI 2.103.3 **portátil** (zip oficial de `cli/cli`, checksum SHA-256 conferido) em `%LOCALAPPDATA%\Programs\GitHubCLI`, sem administrador e sem mexer no PATH; login guardado pelo próprio `gh` (`%APPDATA%\GitHub CLI`), para o `publicar.ps1` | `& "$env:LOCALAPPDATA\Programs\GitHubCLI\bin\gh.exe" auth logout`, depois apagar `%LOCALAPPDATA%\Programs\GitHubCLI` e `%APPDATA%\GitHub CLI` |
| 27/10/2026 | Instalado scrcpy 5.0.1 via winget (escopo do usuário; pasta do pacote adicionada ao PATH do usuário), para a ação "Espelhar TV" | `winget uninstall --id Genymobile.scrcpy --exact` |
| 28/10/2026 | Atalho `WinHome.lnk` na área de trabalho (`C:\Users\predator\Desktop`), apontando para a versão instalada, para reabrir a Home depois de "Sair" | apagar o atalho da área de trabalho |

## Alterações que o próprio WinHome faz
| O quê | Quando | Como desfazer |
|---|---|---|
| Valor `WinHome` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Só com `"iniciarComWindows": true` | Pôr `false` na config, rodar `.\scripts\desinstalar.ps1` ou `reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v WinHome /f` |
| Pasta `%APPDATA%\WinHome` (config) | Primeira execução | Apagar a pasta (o `desinstalar.ps1` pergunta antes) |

## Alterações que o instalador (`instalador\instalar.ps1`, pacote do `empacotar.ps1`) faz em outro PC
| O quê | Como desfazer |
|---|---|
| Programa em `%LOCALAPPDATA%\WinHome\app` (e dados do WebView2 em `%LOCALAPPDATA%\WinHome\WebView2`) | `desinstalar.cmd` do pacote, ou apagar `%LOCALAPPDATA%\WinHome` com o WinHome fechado |
| Atalho `WinHome.lnk` no menu Iniciar (`%APPDATA%\Microsoft\Windows\Start Menu\Programs`) | `desinstalar.cmd` ou apagar o atalho |
| Valor `WinHome` em `HKCU\...\Run` (só se respondido "s") | `desinstalar.cmd`, ou desligar em Configurações → Geral |
| Remove a marca "baixado da internet" dos arquivos do WinHome copiados (`Unblock-File`) | não se aplica (só os arquivos do próprio WinHome) |
