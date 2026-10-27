# Backlog do WinHome (atualizado em 26/10/2026)

## Em andamento
### v0.4-a — Projetos recentes (código pronto, **falta testar**)
- Branch local `v0.4-a-projetos` (não enviada ao GitHub; a `main` só tem versões testadas).
- Painel `projetos`: junta os recentes do IntelliJ (`%APPDATA%\JetBrains\IntelliJIdea*\options\recentProjects.xml`),
  do Android Studio (`%APPDATA%\Google\AndroidStudio*\...`), do VS Code (`%APPDATA%\Code\User\workspaceStorage\*\workspace.json`)
  e as subpastas com git de `D:\Documents\dev`. Mostra branch do git, WSL e quando foi aberto.
- Clique abre na IDE de origem; botão direito: abrir no VS Code / IntelliJ / Android Studio / Terminal / Explorer, fixar, ocultar.
- Configurações → Projetos: toggle de exibir, editor padrão, máximo, fontes, pastas, ocultos, caminhos dos programas.
- **Para retomar:** `git switch v0.4-a-projetos`, `.\scripts\executar.ps1`, ligar "Mostrar os projetos recentes na Home" e testar:
  1. Aparecem tvlauncher, winhome, os do IntelliJ (inclusive os do WSL/Ubuntu) e os do VS Code, sem repetição.
  2. Clique abre na IDE certa; botão direito → Terminal e Explorer.
  3. Projeto do WSL abre no VS Code em modo remoto (`--folder-uri vscode-remote://wsl+Ubuntu/...`).
  4. Fixar/Desafixar e Ocultar/Mostrar de novo.
  - Aprovado: merge na `main` (`git switch main`, `git merge v0.4-a-projetos`), commit de docs e push.

## Próximas etapas (decisões já tomadas com o usuário)
| Etapa | O quê | Decisões |
|---|---|---|
| v0.4-b | **Tarefas** | Lista simples (Enter adiciona, marcar conclui, limpar concluídas); dados em `%APPDATA%\WinHome\tarefas.json`, fora da config |
| v0.4-c | **Ações rápidas** | Prontas: **Dia de trabalho** (abre VS Code, IntelliJ, Teams, Outlook, Edge), **Conectar TV (ADB)** (`adb connect 192.168.15.11:5555`, só conecta), **Abrir projetos** (`D:\Documents\dev` no Explorer e no Terminal). **Sempre** confirmar mostrando o comando exato |
| v0.4-d | **Dispositivos** | TV `192.168.15.11:5555` e box `192.168.15.13:5555`: só teste de conexão TCP + latência, a cada minuto; nenhum comando nos aparelhos |
| v0.4-e | **Agenda** | **Google Agenda** via "Endereço secreto no formato iCal": hoje e amanhã. O link é secreto: fica só na config local, nunca em git/log; avisar na tela. Registrar `calendar.google.com` como serviço autorizado no CLAUDE.md |
| v0.5 | Modo "área de trabalho" | Home atrás das janelas, no lugar do papel de parede, com toggle |
| v1.0 | Acabamento | Backup/restauração da config, temas prontos, build Release/instalador simples |

## A verificar com o usuário
- Ainda sem confirmação explícita: busca ao digitar ("calc" + Enter), menu de contexto nos apps,
  clique e menu do ícone da bandeja, Home encolhendo com as Configurações abertas.
- Cotações: a AwesomeAPI informou variação do dia de ~8% no dólar (`pctChange`), alta demais para um dia.
  Decidir: manter, trocar por variação semanal ou remover.
- "Iniciar com o Windows" continua **desligado** (Configurações → Geral) — ligar quando quiser.

## Observações conhecidas
- A Wikipédia em português nem sempre tem o fato esperado (ex.: estreia de *O Exterminador do Futuro*, 26/10/1984, não está lá).
- Salvar pelas Configurações regrava o `config.json` sem comentários (anterior em `config.json.bak`).
- Testes pelo Claude: abrir o app com `run_in_background`; print de janela sai preto (pedir print ao usuário).
- PowerShell 5.1: mensagens de commit com aspas → usar `git commit -F arquivo`.
