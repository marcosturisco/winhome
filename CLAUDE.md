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
  Open-Meteo (clima e busca de cidades), AwesomeAPI (cotações), Wikipédia "Neste dia" (curiosidades, `pt.wikipedia.org/api/rest_v1/feed/onthisday/all/MM/DD`),
  YouTube (autorizado em 27/10/2026; painel Vídeos: player oficial `youtube-nocookie.com` num WebView2, só quando o usuário
  abre um link; dados do WebView2 em `%LOCALAPPDATA%\WinHome\WebView2`; não passa pelo `Rede.cs`),
  Google Agenda (autorizado em 27/10/2026; painel Agenda: "Endereço secreto no formato iCal" de `calendar.google.com`,
  a cada 15 min pelo `Rede.cs`). **O endereço é secreto**: fica só no `config.json` local; nunca em git, log, mensagem
  de erro, nome de arquivo (o cache usa hash) nem no chat.
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
- **Visual dos painéis vem da Aparência** (`Estilo`): fonte `estilo.Fonte`; tamanhos de texto e de ícones de conteúdo
  só pelos papéis `estilo.Legenda/Corpo/Realce/Titulo/Grande` ou `estilo.Tam(n)` (escalam com `tema.tamanhoTexto`) —
  nunca `FontSize` fixo em painel; cores, cantos e espaçamento também do `Estilo`. Ficam fixos só a moldura
  (alças, botões do cabeçalho, barra da janela), diálogos e a tela de Configurações.
- Abrir rápido e gastar pouca memória; ícones com cache em memória.

## Comandos
| Ação | Comando |
|---|---|
| Compilar | `.\scripts\compilar.ps1` (ou `dotnet build src\WinHome -c Debug`) |
| Compilar e abrir | `.\scripts\executar.ps1` |
| Remover inicialização e fechar | `.\scripts\desinstalar.ps1` |
| Pacote de instalação (outro PC) | `.\scripts\empacotar.ps1` → `dist\WinHome-<versão>.zip` (Release self-contained + `instalador\`) |
| Publicar versão no GitHub | `.\scripts\publicar.ps1 -Versao 1.2.3` (versão no csproj, pacote, tag, push e Release com o .zip; notas = `docs\notas-release.md` + commits). Precisa do `gh` portátil logado |
| Push para o GitHub | `git -c "core.sshCommand='C:/Program Files/Git/usr/bin/ssh.exe' -F none -i C:/Users/predator/.ssh/id_ed25519 -o IdentitiesOnly=yes" push` |

O push usa a chave passada **só no comando** (não mexer no gitconfig global nem no `~/.ssh/config`).

## Estrutura
- `src/WinHome/` — `Program` (instância única), `App`, `MainWindow` (janela, fundo, atalho)
  - `Configuracao/` — modelo da config, serviço que carrega/observa/salva o arquivo, `EditorConfig` (operações de edição)
  - `Paineis/` — `HomeBuilder` (grade e resultados da busca) e um arquivo por tipo de painel
  - `Telas/` — `PainelConfiguracoes` (painel lateral de Configurações)
  - `Servicos/` — abrir apps, ícones do shell, apps instalados, atalho global, inicialização com o Windows
- `scripts/` — compilar, executar, desinstalar, empacotar
- `instalador/` — vai dentro do pacote: `instalar.ps1/.cmd`, `desinstalar.ps1/.cmd`, `LEIA-ME.txt` (sem administrador;
  programa em `%LOCALAPPDATA%\WinHome\app`). O `WinHome.csproj` copia `WinHome.pri` e `*.xbf` na publicação (sem eles
  o Release fecha ao abrir).
- `docs/` — plano, **backlog (pendências e próximo passo)**, opções da config (`configuracao.md`), reversão

## Testar
- Abrir pelo PowerShell da ferramenta com `run_in_background` (senão o processo cai junto com o comando).
- Neste PC o uso diário é a **versão instalada** (`%LOCALAPPDATA%\WinHome\app`, atualizada pelos Releases); a de
  desenvolvimento (`bin\Debug`) é só para testes e usa a mesma config. Com a instalada presente, o "iniciar com o
  Windows" aponta sempre para ela. Depois de testar com o `executar.ps1`, sair (`--sair`) e reabrir a instalada.
- Print de janela com `PrintWindow` sai preto (WinUI); peça um print ao usuário.
- "Sair do WinHome" (menu da bandeja ou Configurações → Geral) **encerra** o app (o `Close()` não passa
  pelo `AppWindow.Closing`); `Esc`/`✕`/Alt+F4 só escondem; `—` minimiza; `❐` alterna para o modo janela
  e de volta; a faixa do topo arrasta a Home para outro monitor; monitor/modo ficam em `%APPDATA%\WinHome\estado.json`.
- Mensagens Win32 da janela (atalho, bandeja) passam por `MensagensJanela`; ícone gerado por `scripts/gerar-icone.ps1`.
