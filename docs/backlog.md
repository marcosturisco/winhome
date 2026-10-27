# Backlog do WinHome (atualizado em 27/10/2026)

## Concluído recentemente
- **v0.4-a — Projetos recentes** ✅ (27/10/2026): recentes do IntelliJ, Android Studio e VS Code
  + subpastas com git de `D:\Documents\dev`; abrir na IDE de origem, Terminal, Explorer, WSL remoto, fixar/ocultar.
- **v0.4-b — Tarefas** ✅ (27/10/2026): painel `tarefas` (dados em `%APPDATA%\WinHome\tarefas.json`), adicionar,
  concluir, editar, mover, limpar concluídas.
  - Extras pedidos na mesma etapa: botão **Pastas…** no painel Projetos (trocar/remover/adicionar pasta pela Home);
    vários monitores — botões [⚙][—][❐][✕], faixa do topo arrasta a Home para qualquer monitor (mesmo ocupando a tela),
    último monitor e modo lembrados em `%APPDATA%\WinHome\estado.json`.

## Em andamento
### v0.4-c — Ações rápidas (código pronto, **falta testar**)
- Branch local `v0.4-c-acoes` (não enviada ao GitHub).
- Painel `acoes` + `acoesRapidas` na config (passos `app`, `editor`, `caminho`/`args`, `esperar`, `{pastaProjetos}`);
  confirmação com os comandos exatos; seção Configurações → Ações rápidas.
- **Para retomar:** `git switch v0.4-c-acoes`, `.\scripts\executar.ps1` e testar:
  1. Configurações → Ações rápidas → ligar "Mostrar as ações rápidas na Home".
  2. Cada botão abre a confirmação com os comandos exatos; Cancelar/Esc não executa nada.
  3. Dia de trabalho: abre os 5 apps e a Home se esconde. Abrir projetos: Explorador + Terminal na pasta.
  4. Conectar TV: mostra a resposta do adb no topo da Home (TV ligada ou desligada).
  5. Configurações: renomear, trocar ícone, mover, adicionar app/comando, remover, restaurar as padrão.
  - Aprovado: merge na `main`, commit de docs e push.

## Próximas etapas (decisões já tomadas com o usuário)
| Etapa | O quê | Decisões |
|---|---|---|
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
