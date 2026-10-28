# WinHome

Uma tela inicial para o Windows 11: relógio, clima, cotações, seus apps, tarefas, agenda e outros painéis,
tudo configurável. Abre ao entrar no Windows e some/volta com `Ctrl+Alt+Space`. Não troca nada do Windows:
barra de tarefas, menu Iniciar e área de trabalho continuam iguais.

**[⬇ Baixar a versão mais recente](https://github.com/marcosturisco/winhome/releases/latest)**

## Instalar (uns 5 minutos)

**Você precisa de:** Windows 11 de 64 bits e uns 250 MB livres. Não precisa de administrador nem instalar
nada antes (.NET e Windows App SDK já vão dentro do pacote).

1. **Baixe o pacote.** Abra [a página da versão mais recente](https://github.com/marcosturisco/winhome/releases/latest)
   e, em *Assets*, clique em **`WinHome-<versão>.zip`** (uns 90 MB).
2. **Extraia tudo.** Na pasta Downloads, clique com o botão direito no .zip → **Extrair tudo** → **Extrair**.
   Não rode o instalador de dentro do .zip: ele precisa da pasta `app` ao lado.
3. **Rode o instalador.** Na pasta extraída, dê duplo clique em **`instalar.cmd`**. Abre uma janela preta: é o
   instalador.
4. **Se aparecer "O Windows protegeu o computador":** clique em **Mais informações** → **Executar assim mesmo**.
   O aviso aparece porque o programa não tem assinatura digital paga.
5. **Responda as perguntas** digitando `s` (sim) ou só `Enter` (não):

   | Pergunta | Recomendado |
   |---|---|
   | Abrir o WinHome ao entrar no Windows? | `s` |
   | Criar atalho do WinHome na área de trabalho? | `s` (reabre a Home se você sair dela) |
   | Restaurar um backup do WinHome? | `Enter`, a não ser que alguém tenha te passado um backup ([veja abaixo](#levar-a-configuração-para-outro-pc)) |

6. **Pronto.** O instalador lista os programas opcionais que faltam ([veja abaixo](#programas-opcionais)) e abre
   a Home. Pode fechar a janela preta e apagar o .zip e a pasta extraída.

O programa fica em `%LOCALAPPDATA%\WinHome\app` e as suas configurações em `%APPDATA%\WinHome`.

## Primeiros passos

| Para… | Faça |
|---|---|
| Mostrar ou esconder a Home | `Ctrl+Alt+Space`, ou clique no ícone do WinHome na bandeja (perto do relógio; se não estiver lá, na setinha `^`) |
| Configurar (tema, cidades do clima, painéis, apps…) | Engrenagem `⚙` no canto superior direito. Tudo muda na hora |
| Abrir um app | Clique nele, ou comece a digitar para buscar em todos os apps instalados e tecle `Enter` |
| Organizar um app | Botão direito nele: mover, trocar de grupo, remover |
| Esconder | `Esc` ou `✕`. A Home continua rodando e volta pelo atalho ou pela bandeja |
| Minimizar / modo janela | `—` vai para a barra de tarefas; `❐` alterna entre tela inteira e janela |
| Levar para outro monitor | Arraste pela faixa do topo (duplo clique alterna tela inteira/janela) |
| Sair de vez | Botão direito no ícone da bandeja → **Sair do WinHome** (ou Configurações → Geral). Para abrir de novo: atalho da área de trabalho ou menu Iniciar → **WinHome** |

Dicas de configuração:
- **Configurações → Aparência → Temas prontos** troca todo o visual com um clique.
- **Configurações → Geral**: seu nome na saudação, tecla de atalho, iniciar com o Windows e o
  **modo área de trabalho** (a Home fica atrás das janelas, no lugar do papel de parede).
- **Configurações → Agenda**: mostra o seu Google Agenda. Cole o "Endereço secreto no formato iCal"
  (Google Agenda → Configurações da agenda → Integrar agenda). Esse endereço fica só no seu PC.
- Quem preferir pode editar `%APPDATA%\WinHome\config.json` à mão ([todas as opções](docs/configuracao.md)).

## Programas opcionais

A Home funciona sem eles; só alguns painéis precisam. Para instalar, abra o **Terminal** (botão direito no
menu Iniciar → Terminal) e cole o comando.

| Programa | Usado em | Comando |
|---|---|---|
| VLC 64 bits | painel Vídeos | `winget install VideoLAN.VLC` |
| adb | TV e Dispositivos | `winget install Google.PlatformTools` |
| scrcpy | espelhar a TV | `winget install Genymobile.scrcpy` |
| Windows Terminal | Ações rápidas e Projetos | `winget install Microsoft.WindowsTerminal` |
| VS Code | Projetos | `winget install Microsoft.VisualStudioCode` |

IntelliJ e Android Studio são encontrados sozinhos se estiverem instalados.

## Atualizar

Baixe o .zip da [versão mais recente](https://github.com/marcosturisco/winhome/releases/latest), extraia e rode
o `instalar.cmd` de novo. Ele fecha a Home, atualiza por cima e **mantém as suas configurações**.

## Levar a configuração para outro PC

1. No PC antigo: **Configurações → Backup → Fazer backup…** e guarde o `.zip`.
2. No PC novo: rode o instalador e responda `s` em "Restaurar um backup?", escolhendo esse `.zip`
   (ou depois, em **Configurações → Backup → Restaurar backup…**).
3. Se você usa a Agenda, cole de novo o endereço secreto em **Configurações → Agenda**: por segurança, ele
   não vai no backup.

## Desinstalar

Na pasta extraída do pacote, dê duplo clique em **`desinstalar.cmd`**. Ele fecha a Home, remove os atalhos, o
"iniciar com o Windows" e o programa, e pergunta antes de apagar as suas configurações. Não sobra nada no
Windows.

## Problemas comuns

| Problema | Solução |
|---|---|
| A Home não aparece | Use `Ctrl+Alt+Space` ou o ícone da bandeja. Com mais de um monitor, ela abre no último em que ficou: olhe nas outras telas |
| O atalho não funciona | Outro programa pode usar a mesma combinação. Troque em **Configurações → Geral → Tecla de atalho**. A Home avisa no topo quando não consegue registrar o atalho |
| "Pasta 'app' não encontrada" ao instalar | O .zip não foi extraído inteiro. Volte ao passo 2 (**Extrair tudo**) |
| O instalador mostrou uma mensagem em vermelho | A janela fica aberta até você apertar uma tecla: tire um print da mensagem e mande para quem te passou o link |
| Clima, cotações ou curiosidades sem dados | Precisam de internet. Sem conexão, a Home mostra o último valor salvo e atualiza sozinha quando a internet volta |

---

## Para desenvolvedores

WinUI 3 (Windows App SDK, self-contained, sem MSIX) + .NET 10, C#.

| Ação | Comando |
|---|---|
| Compilar e abrir | `.\scripts\executar.ps1` |
| Gerar o pacote de instalação | `.\scripts\empacotar.ps1` → `dist\WinHome-<versão>.zip` |
| Publicar uma versão no GitHub | `.\scripts\publicar.ps1 -Versao 1.2.3` |
| Remover a inicialização e fechar | `.\scripts\desinstalar.ps1` |

Plano e fases: [docs/plano.md](docs/plano.md) · Opções da config: [docs/configuracao.md](docs/configuracao.md) ·
Pendências: [docs/backlog.md](docs/backlog.md).
