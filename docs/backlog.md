# Backlog do WinHome (atualizado em 27/10/2026)

## Concluído recentemente
- **v0.4-a — Projetos recentes** ✅ (testado e aprovado em 27/10/2026): recentes do IntelliJ, Android Studio e VS Code
  + subpastas com git de `D:\Documents\dev`; abrir na IDE de origem, Terminal, Explorer, WSL remoto, fixar/ocultar.

## Em andamento
### v0.4-b — Tarefas + pastas de projetos pela Home (código pronto, **falta testar**)
- Branch local `v0.4-b-tarefas` (não enviada ao GitHub).
- Painel `tarefas` (dados em `%APPDATA%\WinHome\tarefas.json`) e opção `tarefas.mostrarConcluidas`; seção Configurações → Tarefas.
- Painel Projetos ganhou o botão **Pastas…** (trocar, remover, adicionar pasta sem abrir as Configurações).
- Vários monitores: botões — (minimizar), ❐ (modo janela arrastável ↔ ocupar a tela no monitor atual), ✕ (esconder).
- **Para retomar:** `git switch v0.4-b-tarefas`, `.\scripts\executar.ps1` e testar:
  1. Configurações → Tarefas → ligar "Mostrar as tarefas na Home".
  2. Enter adiciona; marcar risca e manda para o fim; desmarcar volta; ✕ remove; "Limpar concluídas".
  3. Duplo clique / botão direito → Editar (Enter grava, Esc desiste); Mover para cima/baixo.
  4. Fechar e abrir o WinHome: as tarefas continuam lá. Digitar na caixa de tarefas não abre a busca.
  5. Projetos → Pastas…: trocar a pasta, remover, adicionar; a lista se atualiza.
  6. Botões [⚙][—][❐][✕]: — minimiza (volta pela barra de tarefas, atalho ou bandeja); ✕ esconde;
     ❐ vira janela → arrastar pela faixa do topo para o outro monitor → ❐ (ou duplo clique na faixa) ocupa aquele monitor;
     esconder e reabrir pelo atalho mantém o monitor.
  - Aprovado: merge na `main`, commit de docs e push.

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
