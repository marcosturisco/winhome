# Backlog do WinHome (atualizado em 27/10/2026)

## Concluído recentemente
- **v0.4-a — Projetos recentes** ✅ (27/10/2026): recentes do IntelliJ, Android Studio e VS Code
  + subpastas com git de `D:\Documents\dev`; abrir na IDE de origem, Terminal, Explorer, WSL remoto, fixar/ocultar.
- **v0.4-b — Tarefas** ✅ (27/10/2026): painel `tarefas` (dados em `%APPDATA%\WinHome\tarefas.json`), adicionar,
  concluir, editar, mover, limpar concluídas.
  - Extras pedidos na mesma etapa: botão **Pastas…** no painel Projetos (trocar/remover/adicionar pasta pela Home);
    vários monitores — botões [⚙][—][❐][✕], faixa do topo arrasta a Home para qualquer monitor (mesmo ocupando a tela),
    último monitor e modo lembrados em `%APPDATA%\WinHome\estado.json`.
- **v0.4-c — Ações rápidas** ✅ (27/10/2026): painel `acoes` com confirmação mostrando os comandos exatos; ações
  Dia de trabalho, Abrir projetos e da TV (Conectar, Espelhar com scrcpy, Print, Logs, Instalar APK, Desconectar),
  cada uma com descrição; edição em Configurações → Ações rápidas.
  - Extras pedidos na mesma etapa: painel **Vídeos** (VLC instalado dentro do painel via LibVLCSharp.WinUI;
    YouTube pelo player oficial num WebView2 — serviço autorizado); botão ↻ fixo no cabeçalho de Mensagem/Curiosidades.
- **Atalhos nos painéis** ✅ (27/10/2026): ✥ mover arrastando (ou menu início/trás/frente/fim), borda direita ajusta a
  largura em colunas (duplo clique ocupa o espaço livre), ✕ oculta com Desfazer — tudo sem abrir as Configurações.

## Próximo passo
- **v0.4-d — Dispositivos** (decisões abaixo). Fazer numa branch `v0.4-d-dispositivos` e só juntar na `main` depois do teste.

## Próximas etapas (decisões já tomadas com o usuário)
| Etapa | O quê | Decisões |
|---|---|---|
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
