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

## Internet (serviços autorizados)
- Só serviços gratuitos, sem chave e sem login, autorizados pelo usuário:
  Open-Meteo (clima e busca de cidades), AwesomeAPI (cotações), Wikipédia "Neste dia" (curiosidades, v0.3-c).
- Nada pessoal sai do PC (só nome/coordenadas da cidade, códigos de moedas, dia/mês).
- Toda chamada passa por `Servicos/Rede.cs`: tempo limite de 8 s, cache em memória + `%APPDATA%\WinHome\cache\`,
  falha de rede nunca trava nem derruba a Home (mostra o último valor salvo).
- Serviço novo: pedir autorização e registrar aqui.

## Configuração (princípio do projeto)
- Tudo o que for visual ou conteúdo vem de `%APPDATA%\WinHome\config.json` (JSON com comentários `//`).
- Na primeira execução o arquivo é criado a partir de `src/WinHome/Assets/config.padrao.json`.
- A Home recarrega sozinha quando o arquivo muda; JSON inválido mostra o erro e mantém a config anterior.
- A tela de Configurações altera a config via `EditorConfig`/`ConfigService.Alterar` e regrava o arquivo
  (sem comentários; anterior em `config.json.bak`).
- Toda opção nova entra no modelo (`Configuracao/Config.cs`) **com valor padrão**, ganha controle na tela
  de Configurações e é documentada no `config.padrao.json` e em `docs/configuracao.md`.

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
  - `Configuracao/` — modelo da config, serviço que carrega/observa/salva o arquivo, `EditorConfig` (operações de edição)
  - `Paineis/` — `HomeBuilder` (grade e resultados da busca) e um arquivo por tipo de painel
  - `Telas/` — `PainelConfiguracoes` (painel lateral de Configurações)
  - `Servicos/` — abrir apps, ícones do shell, apps instalados, atalho global, inicialização com o Windows
- `scripts/` — compilar, executar, desinstalar
- `docs/` — plano, opções da config (`configuracao.md`), reversão

## Testar
- Abrir pelo PowerShell da ferramenta com `run_in_background` (senão o processo cai junto com o comando).
- Print de janela com `PrintWindow` sai preto (WinUI); peça um print ao usuário.
- "Sair do WinHome" (menu da bandeja ou Configurações → Geral) **encerra** o app (o `Close()` não passa
  pelo `AppWindow.Closing`); `Esc`/`—`/Alt+F4 só escondem.
- Mensagens Win32 da janela (atalho, bandeja) passam por `MensagensJanela`; ícone gerado por `scripts/gerar-icone.ps1`.
