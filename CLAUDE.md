# Projeto: WinHome — Home personalizada para o Windows 11

Tela inicial própria para o PC (Windows 11 Pro), aberta ao fazer login e chamada por tecla de atalho,
com painéis configuráveis (relógio, apps de trabalho, pessoal…). Projeto irmão do launcher da TV
(`D:\Documents\dev\tvlauncher`), mas independente. Plano completo: @docs/plano.md

## Como trabalhar comigo
- Responda em **português do Brasil**, explicando de forma simples o que vai fazer antes de fazer.
- Trabalhe **passo a passo**; em tarefas grandes, apresente o plano antes de editar.
- Código e interface **originais**; não copiar projetos de terceiros.

## Regras de segurança do PC
- **Sem administrador.** Nada de UAC, `HKLM`, serviços, drivers, políticas ou troca do shell (`explorer.exe`/Winlogon).
- A única alteração no sistema permitida é a entrada `WinHome` em
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (iniciar com o Windows), controlada pelo toggle
  `iniciarComWindows`. Qualquer outra alteração no sistema: pedir confirmação e registrar em `docs/reversao.md`
  com o comando para desfazer.
- Instalar/desinstalar programas no PC (winget etc.): só com confirmação.
- Ações que executam comandos (painel "Ações rápidas") sempre pedem confirmação ao usuário dentro da Home.
- Editar arquivos com Edit/Write, não com `Set-Content` do PowerShell 5.1 (corrompe acentos).
  Scripts `.ps1` ficam **sem acentos** (o PS 5.1 lê UTF-8 sem BOM como ANSI).

## Configuração (princípio do projeto)
- Tudo o que for visual ou conteúdo vem de `%APPDATA%\WinHome\config.json` (JSON com comentários `//`).
- Na primeira execução o arquivo é criado a partir de `src/WinHome/Assets/config.padrao.json`.
- A Home recarrega sozinha quando o arquivo muda; JSON inválido mostra o erro e mantém a config anterior.
- Toda opção nova entra no modelo (`Configuracao/Config.cs`) **com valor padrão** e é documentada
  com comentário no `config.padrao.json`.

## Alvo técnico
- Windows 11 (x64), WinUI 3 (Windows App SDK 2.x, self-contained, sem MSIX), .NET 10, C#.
- Interface montada em código a partir da config (`HomeBuilder`); XAML mínimo.
- Abrir rápido e gastar pouca memória; ícones com cache em memória.

## Comandos
| Ação | Comando |
|---|---|
| Compilar | `.\scripts\compilar.ps1` (ou `dotnet build src\WinHome -c Debug`) |
| Compilar e abrir | `.\scripts\executar.ps1` |
| Remover inicialização e fechar | `.\scripts\desinstalar.ps1` |
| Push para o GitHub | `git -c "core.sshCommand='C:/Program Files/Git/usr/bin/ssh.exe' -F none -i C:/Users/predator/.ssh/id_ed25519 -o IdentitiesOnly=yes" push` |

O push usa a chave passada **só no comando** (não mexer no gitconfig global nem no `~/.ssh/config`).

## Estrutura
- `src/WinHome/` — `Program` (instância única), `App`, `MainWindow` (janela, fundo, atalho)
  - `Configuracao/` — modelo da config e serviço que carrega/observa o arquivo
  - `Paineis/` — `HomeBuilder` (monta a grade) e um arquivo por tipo de painel
  - `Servicos/` — abrir apps, ícones do shell, atalho global, inicialização com o Windows
- `scripts/` — compilar, executar, desinstalar
- `docs/` — plano, reversão
