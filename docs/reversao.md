# Registro de alterações no PC e como desfazer

| Data | Alteração | Como desfazer |
|---|---|---|
| 26/10/2026 | Instalado .NET 10 SDK (10.0.403) via winget, para compilar o projeto | `winget uninstall --id Microsoft.DotNet.SDK.10 --exact` |

## Alterações que o próprio WinHome faz
| O quê | Quando | Como desfazer |
|---|---|---|
| Valor `WinHome` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Só com `"iniciarComWindows": true` | Pôr `false` na config, rodar `.\scripts\desinstalar.ps1` ou `reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v WinHome /f` |
| Pasta `%APPDATA%\WinHome` (config) | Primeira execução | Apagar a pasta (o `desinstalar.ps1` pergunta antes) |
