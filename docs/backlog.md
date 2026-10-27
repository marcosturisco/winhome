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
- **App ancorado** ✅ (27/10/2026): painel com a janela real de um programa posicionada sobre ele (`appsAncorados`),
  vários lado a lado; IntelliJ, VS Code, Android Studio, Terminal, "Escolher uma janela aberta…" e "Outro app instalado…";
  acompanha rolagem/✥/↔/altura/monitor, recorta o que sai da tela, fica logo acima da Home, minimiza junto; Soltar ou
  sair devolve a janela. Sem `SetParent`/dono (seguro se o WinHome cair). `WinHome.exe --sair` = saída normal (scripts).

- **v0.4-d — Dispositivos** ✅ (27/10/2026): painel com TV e box (teste TCP da porta, latência, a cada 60 s, sem
  comandos); clique no aparelho = menu com Conectar, Espelhar, Print, Logs, Instalar APK, Desconectar (com o endereço
  do aparelho e a confirmação das Ações rápidas), Testar agora, Copiar endereço.

- **v0.4-e — Agenda Google** ✅ (27/10/2026): painel com hoje/amanhã (dias configurável) de uma ou mais agendas pelo
  "Endereço secreto no formato iCal" (Ical.Net: repetições, exceções, fusos); cores, "agora"/"em X min", clique abre o
  dia no Google, ↻; endereço em campo de senha, cache com hash, nunca exposto. **Fecha a v0.4.**

- **v0.5 — Modo "área de trabalho"** ✅ (27/10/2026): `modoAreaDeTrabalho` deixa a Home no fundo (WM_WINDOWPOSCHANGING →
  HWND_BOTTOM), fora da barra de tarefas e do Alt+Tab; atalho/bandeja trazem à frente, ativar outro programa (ou Esc,
  —, ✕) manda de volta, Win+D não a esconde. Sem truques no shell. Padrões adotados: atalho traz à frente; ícones da
  área de trabalho intocados; monitor = onde a Home está.

## v1.0 — Acabamento ✅ (27/10/2026)
1. **Backup e restauração** ✅ (27/10/2026): Configurações → Backup (.zip com config, tarefas, vídeos; endereços da
   agenda só se marcado; ao restaurar mantém os deste PC; reserva em `antes-da-restauracao-<data>`).
2. **Temas prontos** ✅ (27/10/2026): galeria em Aparência (Padrão, Grafite, Oceano, Claro, Sépia, Alto contraste) + "Salvar o visual atual como tema" (`temasSalvos`); aplica só o visual.
3. **Build Release + pacote de instalação** ✅ (testado em outro notebook): `.\scripts\empacotar.ps1` → `.zip` self-contained + `instalar.ps1` (sem
   administrador, `%LOCALAPPDATA%\WinHome`, atalho no menu Iniciar, iniciar com o Windows opcional, restaurar backup,
   avisar o que falta: VLC, adb/scrcpy, IDEs) + desinstalar.

## Próximas etapas (decisões já tomadas com o usuário)
| Etapa | O quê | Decisões |
|---|---|---|

## A verificar com o usuário
- Ainda sem confirmação explícita: busca ao digitar ("calc" + Enter), menu de contexto nos apps,
  clique e menu do ícone da bandeja, Home encolhendo com as Configurações abertas.
- ~~Cotações: variação do dia de ~8% no dólar~~ → resolvido em 27/10/2026: era erro pontual da AwesomeAPI; a Home
  esconde a variação acima de 5% no dia (exceto BTC/ETH) e mostra só o valor.
- "Iniciar com o Windows" continua **desligado** (Configurações → Geral) — ligar quando quiser.

## Observações conhecidas
- A Wikipédia em português nem sempre tem o fato esperado (ex.: estreia de *O Exterminador do Futuro*, 26/10/1984, não está lá).
- Salvar pelas Configurações regrava o `config.json` sem comentários (anterior em `config.json.bak`).
- Testes pelo Claude: abrir o app com `run_in_background`; print de janela sai preto (pedir print ao usuário).
- PowerShell 5.1: mensagens de commit com aspas → usar `git commit -F arquivo`.
