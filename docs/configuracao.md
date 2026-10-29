# Opções do config.json

Arquivo: `%APPDATA%\WinHome\config.json`. Tudo pode ser mudado pela engrenagem da Home (Configurações);
editar o arquivo à mão também funciona: a Home recarrega ao salvar.

> Ao salvar pela tela de Configurações, o arquivo é regravado sem os comentários `//`.
> A versão anterior fica sempre em `config.json.bak`. O arquivo comentado original está em
> `src/WinHome/Assets/config.padrao.json`.

## Geral
| Opção | Valores | Padrão |
|---|---|---|
| `nome` | texto da saudação ("Bom dia, …") | `""` |
| `janela` | `maximizada` (mantém a barra de tarefas) · `telaCheia` | `maximizada` |
| `atalho` | ex.: `Ctrl+Alt+Space`, `Ctrl+Shift+H`, `Alt+F1` (modificadores: Ctrl, Alt, Shift, Win) | `Ctrl+Alt+Space` |
| `iniciarComWindows` | `true` cria o valor `WinHome` em `HKCU\...\Run`; `false` remove | `false` |
| `esconderAoAbrirApp` | esconde a Home depois de abrir um app | `true` |
| `mostrarBusca` | caixa de busca no topo (e digitar em qualquer lugar busca) | `true` |
| `iconeNaBandeja` | ícone na bandeja do sistema: clique mostra a Home; botão direito tem Mostrar, Configurações e Sair | `true` |
| `modoAreaDeTrabalho` | a Home fica **atrás das janelas**, como um papel de parede: sai da barra de tarefas e do Alt+Tab; o atalho/bandeja a trazem para a frente e clicar em outro programa (ou Esc, ✕) a manda de volta; com ela já no fundo, o ✕ a esconde de verdade (volta pelo atalho/bandeja) e o botão — fica oculto; se algo a minimizar (Win+D), volta sozinha. Não altera nada no Windows | `false` |

## Tema (`tema`)
| Opção | Valores | Padrão |
|---|---|---|
| `modo` | `escuro` · `claro` · `sistema` | `escuro` |
| `corDestaque` | `#RRGGBB` ou `#AARRGGBB` (títulos e relógio) | `#4F8CFF` |
| `fundo` | `mica` · `micaAlt` · `acrilico` · `cor` · `imagem` | `mica` |
| `corFundo` | cor usada com `fundo: cor` | `#101418` |
| `imagemFundo` | caminho da imagem com `fundo: imagem` (aceita `%VARIAVEIS%`) | `""` |
| `fonte` | nome de fonte instalada (vale para todo texto dos painéis) | `Segoe UI Variable` |
| `tamanhoTexto` | texto normal dos painéis, 10 a 24; todos os tamanhos acompanham na mesma proporção (legenda 12, corpo 14, realce 16, título 18, grande 20, hora 72… no padrão) | `14` |
| `cantos` | arredondamento dos painéis (px) | `12` |
| `tamanhoIcone` | 16 a 256 | `48` |
| `opacidadePaineis` | 0 (transparente) a 1 (sólido) | `0.6` |
| `espacamento` | distância entre painéis (px) | `16` |
| `colunas` | colunas da grade (1 a 24) | `6` |

### Temas prontos e salvos (`temasSalvos`)
Configurações → Aparência → **Temas prontos**: Padrão, Grafite, Oceano, Claro, Sépia e Alto contraste, mais os seus.
Clicar aplica só o visual (`modo`, `corDestaque`, `fundo`, `corFundo`, `fonte`, `cantos`, `opacidadePaineis`);
`colunas`, `espacamento`, `tamanhoTexto` e `tamanhoIcone` ficam como estão. **Salvar o visual atual como tema…** guarda em
`temasSalvos` (`{ nome, tema }`; mesmo nome substitui); botão direito na miniatura remove.

## Boas-vindas (`boasVindas`)
Painel do topo (`id: "boasVindas"`; configs antigas com `"relogio"` são convertidas ao carregar).
| Opção | Valores | Padrão |
|---|---|---|
| `mostrarSaudacao` | "Bom dia, …" | `true` |
| `mostrarData` | data por extenso | `true` |
| `mostrarSegundos` | hora com segundos | `false` |
| `clima.visivel` | cartões de clima à direita | `true` |
| `clima.unidade` | `C` · `F` | `C` |
| `clima.cidades` | lista de `{ nome, regiao, pais, latitude, longitude, fuso }` (adicione pela busca em Configurações → Boas-vindas) | São Paulo e Orlando |
| `cotacoes.visivel` | cartão de cotações | `true` |
| `cotacoes.moedas` | códigos em relação ao real: `USD`, `EUR`, `GBP`, `JPY`, `CAD`, `AUD`, `CHF`, `CNY`, `ARS`, `BTC`, `ETH`. Cada uma mostra valor e variação do dia (▲/▼); variação acima de 5% no dia é tratada como erro da API e escondida (exceto `BTC` e `ETH`) | `["USD", "EUR"]` |

Clima e cotações vêm da internet ([Open-Meteo](https://open-meteo.com) a cada 30 min; [AwesomeAPI](https://docs.awesomeapi.com.br) a cada 15 min)
e ficam em cache em `%APPDATA%\WinHome\cache\`. Sem conexão, os cartões mostram o último valor salvo.

## Mensagens (`mensagens`)
Painel `id: "mensagem"`: uma frase por dia (a mesma o dia todo); o botão ↻ mostra outra.
| Opção | Valores | Padrão |
|---|---|---|
| `fonte` | `ambas` · `embutidas` (lista do WinHome, `Assets/mensagens.json`) · `minhas` | `ambas` |
| `minhas` | lista de `{ "texto": "...", "autor": "..." }` | `[]` |

## Curiosidades (`curiosidades`)
Painel `id: "curiosidade"`: um fato da data de hoje com foto, da [Wikipédia em português](https://pt.wikipedia.org) ("Neste dia").
Primeiro os destacados pela Wikipédia, depois os demais eventos; ↻ mostra outro. Uma busca por dia (cache de 12 h).
| Opção | Valores | Padrão |
|---|---|---|
| `incluirNascimentos` | inclui quem nasceu nesta data | `false` |
| `incluirMortes` | inclui quem morreu nesta data | `false` |

## Projetos (`projetos`)
Painel `id: "projetos"`: projetos abertos recentemente no IntelliJ, Android Studio e VS Code, mais as subpastas
com git das `pastas`, sem repetição. Mostra a branch do git, se é do WSL e quando foi aberto.
Clique abre no editor padrão; botão direito: abrir no VS Code / IntelliJ / Android Studio / Terminal / Explorer, fixar, ocultar.
Projetos do WSL abrem no VS Code em modo remoto. Nada sai do PC: tudo é lido dos arquivos locais das IDEs.
O botão **Pastas…** no rodapé do painel troca, remove ou adiciona pastas sem abrir as Configurações.
| Opção | Valores | Padrão |
|---|---|---|
| `usarIntellij` | lê `%APPDATA%\JetBrains\IntelliJIdea*\options\recentProjects.xml` | `true` |
| `usarAndroidStudio` | lê os recentes do Android Studio (`%APPDATA%\Google\AndroidStudio*`) | `true` |
| `usarVsCode` | lê `%APPDATA%\Code\User\workspaceStorage\*\workspace.json` | `true` |
| `pastas` | pastas cujas subpastas com git entram na lista | `["D:\\Documents\\dev"]` |
| `maximo` | quantos projetos mostrar | `8` |
| `editorPadrao` | `origem` (IDE onde foi aberto) · `vscode` · `intellij` · `androidStudio` | `origem` |
| `fixados` | caminhos sempre no topo | `[]` |
| `ocultos` | caminhos que não aparecem (Configurações → Projetos mostra de novo) | `[]` |
| `editores.vsCode`, `editores.intellij`, `editores.androidStudio` | caminho do executável; vazio = detectar sozinho | `""` |

## Tarefas (`tarefas`)
Painel `id: "tarefas"`: lista de tarefas. Enter adiciona; marcar conclui (fica riscada, no fim); ✕ remove;
duplo clique ou botão direito edita, move para cima/baixo ou remove; "Limpar concluídas" apaga as marcadas.
A lista fica em `%APPDATA%\WinHome\tarefas.json`, **fora** da config (as Configurações não a regravam).
Se esse arquivo estiver inválido, a Home começa uma lista nova e guarda o antigo em `tarefas.json.erro`.
| Opção | Valores | Padrão |
|---|---|---|
| `mostrarConcluidas` | mostra as concluídas (riscadas) até limpar | `true` |

## Ações rápidas (`acoesRapidas`)
Painel `id: "acoes"`: um botão por ação. O clique **sempre** abre uma confirmação com cada comando exato, na ordem;
Enter executa, Esc cancela. Abrindo só apps, a Home se esconde depois (como ao abrir um app); comandos com
`esperar` mostram a resposta no topo da Home; erros aparecem como aviso.
Edite em Configurações → Ações rápidas (nome, ícone, descrição, ordem, passos de app ou de comando, restaurar as
padrão ou só adicionar as padrão que faltam).

Cada ação: `nome`, `icone` (um emoji; `Win+.` abre o seletor), `descricao` (para que serve: aparece na dica do botão
e no topo da confirmação), `tipoArquivo` (filtro quando um passo usa `{arquivo}`, ex.: `.apk`) e `passos`. Cada passo:
| Campo | Uso |
|---|---|
| `nome` | texto na confirmação e na dica do botão |
| `app` | AppID do menu Iniciar (como nos grupos de apps) |
| `editor` | `vscode` · `intellij` · `androidStudio`: encontrado como no painel Projetos (sobrevive a atualizações da IDE) |
| `caminho`, `args` | programa, pasta ou documento e argumentos; aceitam `%VARIAVEIS%`. Programa sem pasta (`adb`, `scrcpy`) é procurado no PATH atual do usuário |
| `esperar` | `true`: roda escondido, espera o fim (até 2 min) e mostra a saída (ex.: `adb connect`); se falhar, os passos seguintes são pulados |

Marcadores em `caminho` ou `args`:
| Marcador | Vira |
|---|---|
| `{pastaProjetos}` | cada pasta de `projetos.pastas` (o passo se repete por pasta) |
| `{imagens}` | `Imagens\WinHome` (criada se não existir) |
| `{dataHora}` | data e hora da execução (`2026-10-27_14-30-05`), igual em todos os passos da ação |
| `{arquivo}` | arquivo escolhido ao clicar na ação (filtro `tipoArquivo`); cancelar a escolha cancela a ação |

Programas `.exe` abrem com a variável `ADB` apontando para o `adb` do PATH: o scrcpy traz um adb próprio que, se usado,
reiniciaria o servidor do adb e derrubaria a conexão sem fio.

Ações padrão:
| Ação | O que faz |
|---|---|
| 💼 Dia de trabalho | abre VS Code, IntelliJ, Teams, Outlook e Edge |
| 📁 Abrir projetos | abre a pasta de projetos no Explorador e no Terminal |
| 📺 Conectar TV | `adb connect 192.168.15.11:5555`: a TV aparece no Android Studio e aceita comandos do adb |
| 🪞 Espelhar TV | conecta e abre o **scrcpy**: tela da TV numa janela, controlada por mouse e teclado |
| 📸 Print da TV | conecta, captura a tela, copia para `Imagens\WinHome\tv-<dataHora>.png` e abre |
| 📜 Logs da TV | conecta e abre o Terminal com `adb logcat` ao vivo |
| 📦 Instalar APK na TV | pede um `.apk` e roda `adb install -r` (atualiza mantendo os dados) |
| 🔌 Desconectar TV | `adb disconnect 192.168.15.11:5555` |

## Vídeos (`videos`)
Painel `id: "videos"`. Cole um link e tecle Enter, use o botão de arquivo ou arraste um arquivo/link para a tela.
- **Arquivos e links** (mp4, mkv, avi, mp3, flac, rádios, listas m3u/m3u8…) tocam no **VLC instalado**, dentro do painel:
  tocar/pausar, parar, barra de posição, sem som, volume e ⤢ (abre no VLC em tela cheia, do mesmo ponto).
- **Links do YouTube** (`watch?v=`, `youtu.be/`, `shorts/`, `live/`, playlists com `list=`, início com `t=`) tocam no
  **player do YouTube** (`youtube-nocookie.com`, num WebView2). "Abrir no YouTube" leva ao navegador no mesmo ponto.
- Recentes (botão de relógio) e volume ficam em `%APPDATA%\WinHome\videos.json`, fora da config. No fim do menu de
  recentes, **Limpar o histórico** (com confirmação) apaga a lista.
- **Parar** (VLC ou YouTube) encerra o player, tira a imagem do vídeo e esquece o que estava aberto: o painel volta ao
  estado inicial e a Home não retoma esse vídeo.
- Mudar a config remonta a Home; o vídeo continua do mesmo ponto (pisca por um instante).

| Opção | Valores | Padrão |
|---|---|---|
| `repetir` | recomeça ao terminar (VLC) | `false` |
| `pausarAoEsconder` | pausa ao esconder ou minimizar a Home; o VLC de um vídeo pausado é liberado (memória) e o ▶ retoma do mesmo ponto | `true` |
| `pastaVlc` | pasta do VLC 64 bits (com `libvlc.dll`); vazio = `C:\Program Files\VideoLAN\VLC` | `""` |

## Agenda (`agenda`)
Painel `id: "agenda"`: compromissos de **Hoje**, **Amanhã** e dos dias seguintes (`dias`), de uma ou mais agendas do
Google Agenda, cada uma com sua cor. Repetições, exceções e fusos são resolvidos (biblioteca Ical.Net). Os que já passaram
ficam apagados; o atual mostra "agora" e o próximo "em 25 min". Clique abre o dia no Google Agenda (navegador);
↻ no cabeçalho lê de novo. Atualiza a cada 15 min; sem internet mostra a última leitura (cache em
`%APPDATA%\WinHome\cache\agenda_<hash>…`).

Onde pegar o endereço: Google Agenda no navegador → ⚙ Configurações → clique na agenda (à esquerda) → **Integrar
agenda** → **Endereço secreto no formato iCal**. Cole em Configurações → Agenda (campo escondido; botão **Testar**).
> **O endereço é secreto**: quem o tiver lê a sua agenda. Ele fica só no `config.json` (e no `config.json.bak`) deste PC.
> Se vazar, use "Redefinir" no Google Agenda e cole o novo.

| Opção | Valores | Padrão |
|---|---|---|
| `dias` | dias mostrados a partir de hoje (1 a 7 nas Configurações; até 14 no arquivo) | `2` |
| `calendarios` | `{ nome, endereco, cor }` (`endereco` aceita `https://` ou `webcal://`) | `[]` |

## Dispositivos (`dispositivos`)
Painel `id: "dispositivos"`: uma linha por aparelho com 🟢 latência em ms / 🔴 sem resposta e quando foi o último teste.
O teste só **abre e fecha uma conexão TCP** na porta (limite de 2 s), em paralelo, a cada `intervalo` segundos e ao
abrir a Home; nenhum comando é enviado. **Clicar no aparelho** abre o menu: Conectar, Espelhar (scrcpy), Print,
Logs ao vivo, Instalar APK e Desconectar — geradas com o endereço do aparelho e **sempre** com a confirmação das
Ações rápidas — mais "Testar agora" e "Copiar endereço".
| Opção | Valores | Padrão |
|---|---|---|
| `intervalo` | segundos entre os testes (15 a 3600; Configurações: 15 a 300) | `60` |
| `lista` | `{ nome, icone, endereco, porta }` | 📺 TV `192.168.15.11:5555`, 📡 Box `192.168.15.13:5555` |

## Baterias (`baterias`)
Painel `id: "baterias"`: aparelhos Bluetooth que informam a bateria ao Windows (teclado, mouse, fones… — os mesmos de
Configurações → Bluetooth e dispositivos), **sem configurar nada**: aparecem sozinhos. Cada linha tem o ícone do tipo
de aparelho, a bolinha da faixa (🟢 boa, 🟠 abaixo de `media`, 🔴 abaixo de `baixa`), o nome, o percentual e o ícone de
bateria; conectados primeiro, o mais descarregado em cima; desconectados ficam apagados com a hora da última leitura.
O botão do cabeçalho abre o Bluetooth do Windows.
- Leitura **local** (propriedades do dispositivo no Windows), a cada minuto e ao abrir a Home; sem rede, sem administrador.
  Um aparelho com várias entradas no Windows (fone: áudio, microfone, cada lado) vira uma linha só, com o **menor** nível.
- **Aviso:** ao ficar abaixo de `baixa` (só aparelho conectado), uma notificação do Windows ("🔋 MX Master 3S: 18%")
  — **uma vez por descarga** (volta a avisar depois de carregar até `baixa` + 5%) e mesmo com a Home escondida.
  Clicar na notificação mostra a Home. Com o ícone da bandeja desligado, ele aparece só enquanto o aviso está na tela.
- **No Boas-vindas:** cartão vermelho "Bateria baixa" com os aparelhos abaixo de `baixa`; some quando não há nenhum.

| Opção | Valores | Padrão |
|---|---|---|
| `baixa` | % abaixo do qual fica vermelho e avisa (Configurações: 5 a 50) | `20` |
| `media` | % abaixo do qual fica laranja (Configurações: 10 a 90) | `50` |
| `avisar` | notificação do Windows | `true` |
| `alertaNoBoasVindas` | cartão "Bateria baixa" no Boas-vindas | `true` |
| `mostrarDesconectados` | mostrar os desconectados (apagados, último valor) | `true` |
| `ocultos` | nomes como o Windows mostra; não aparecem nem avisam | `[]` |
| `apelidos` | nome no Windows → nome na Home (ex.: `"AULA-F75 5.0 KB": "Teclado"`) | `{}` |

## Apps ancorados (`appsAncorados`)
Painel com a **janela real de um programa** posicionada sobre a área dele: acompanha rolagem, ✥ (lugar), ↔ (largura),
a alça da base (altura) e a troca de monitor; minimiza junto com a Home; a parte fora da área visível é recortada.
Vários podem ficar lado a lado. Criar: Configurações → Painéis → **+ Adicionar painel → App ancorado**:
IntelliJ IDEA, VS Code, Android Studio, Terminal, **Escolher uma janela aberta…** (qualquer programa aberto; o processo
é descoberto pela janela) ou **Outro app instalado…** (o painel abre o app e ancora a primeira janela nova).
**Soltar** (rodapé do painel) ou sair do WinHome devolvem a janela ao tamanho e lugar de antes. Não usa `SetParent`
nem janela "dona": se o WinHome cair, o programa continua normal (fica onde estava, acessível pela barra de tarefas).

Chave = id do painel (`ancorado1`, `ancorado2`…):
| Campo | Uso |
|---|---|
| `nome` | nome no painel |
| `editor` / `app` / `caminho` + `args` | como abrir (igual às Ações rápidas) |
| `processo` | processo dono da janela (ex.: `idea64`, `Code`, `WindowsTerminal`); vazio = descoberto ao abrir |
| `altura` | altura da área, em pixels da Home (200 a 2000) | 

`WinHome.exe --sair` pede para a Home aberta sair normalmente (usado pelos scripts antes de recompilar).

## Painéis (`paineis`)
Lista em ordem de exibição. Cada item: `id`, `visivel` (true/false), `largura` (colunas), `altura` (pixels; 0 =
automática) e `titulo` (opcional).
- A **altura vale para a linha inteira**: a linha usa a maior `altura` entre os painéis dela (100 a 2000; valores
  abaixo de 100 contam como automática). Automática = altura do conteúdo mais alto da linha.
- Com altura definida, o conteúdo que não cabe **encolhe por igual** até caber (e se rearranja na largura); abaixo de
  50% para de encolher e ganha rolagem dentro do painel. A tela do **Vídeos** encolhe para caber
  (em vez de seguir o 16:9); no **app ancorado** a área (e a janela do app) ocupa o espaço que sobra e a alça de altura
  própria some (a `altura` dele em `appsAncorados` só vale com a linha automática). A alça da linha não desce além da
  **altura mínima do app** (a Home a descobre quando o app recusa encolher); se a janela ainda ficar maior que a área,
  o excesso é recortado para não cobrir os outros painéis.
- `id: "boasVindas"`: saudação, hora e data.
- `id: "mensagem"`: mensagem do dia.
- `id: "curiosidade"`: curiosidades do dia.
- `id: "projetos"`: projetos recentes.
- `id: "tarefas"`: tarefas.
- `id: "acoes"`: ações rápidas.
- `id: "videos"`: vídeos (VLC e YouTube).
- `id: "dispositivos"`: dispositivos (TV, box…).
- `id: "agenda"`: agenda (Google Agenda).
- `id: "baterias"`: baterias dos aparelhos Bluetooth (teclado, mouse, fones…).
- `id: "ancoradoN"`: app ancorado definido em `appsAncorados`.
- Qualquer outro `id`: grupo de apps com o mesmo nome em `grupos`.
- Em Configurações → Painéis, "+ Adicionar painel" cria um grupo de apps ou um painel especial que ainda não esteja na Home.
- **Direto na Home**, sem abrir as Configurações:
  - **✥** no canto do painel (aparece ao passar o mouse): arrastar até outro painel muda a ordem em `paineis` (uma barra
    de destaque mostra se cai antes ou depois dele); botão direito: para o início, para trás, para frente, para o fim.
  - **✕** no canto do painel (aparece ao passar o mouse): oculta o painel (`visivel: false`); o aviso no topo tem **Desfazer**.
  - **Borda direita** do painel (cursor ↔): arrastar muda a `largura`, encaixando nas colunas da grade ("4 de 6 colunas");
    **duplo clique** faz o painel ocupar o espaço livre da linha. Largura que não cabe na linha desce para a seguinte.
  - **Borda de baixo** do painel (cursor ↕): arrastar muda a altura da **linha inteira**, de 10 em 10 pixels
    ("Linha: 320 px"), e grava a `altura` em todos os painéis dela; **duplo clique** volta para a altura automática.

## Grupos de apps (`grupos`)
Cada grupo é uma lista de apps:
| Campo | Uso |
|---|---|
| `nome` | texto embaixo do ícone |
| `app` | AppID do menu Iniciar (veja com `Get-StartApps` no PowerShell) — vale para apps da Store e comuns |
| `caminho` | alternativa ao `app`: `.exe`, atalho, pasta ou documento (aceita `%VARIAVEIS%`) |
| `args`, `pasta` | argumentos e pasta de trabalho (só com `caminho`) |
| `icone` | imagem própria para o ícone (opcional) |

## Backup (Configurações → Backup)
- **Fazer backup…**: `.zip` com `config.json`, `tarefas.json` e `videos.json` (o `estado.json`, com a posição da janela,
  fica de fora). Os endereços secretos da agenda **só entram** marcando "Incluir os endereços secretos da agenda";
  sem isso, saem vazios (e os comentários `//` do `config.json` não vão).
- **Restaurar backup…**: confere o arquivo, pede confirmação, guarda os atuais em
  `%APPDATA%\WinHome\antes-da-restauracao-<data>\` e grava os do backup; a Home se atualiza na hora.
  Agendas do backup sem endereço recebem o deste PC quando há uma agenda com o mesmo nome.

## Teclas
| Tecla | Ação |
|---|---|
| `Ctrl+Alt+Space` (configurável) | mostra/esconde a Home |
| digitar | busca nos grupos e em todos os apps instalados |
| `Enter` na busca | abre o primeiro resultado |
| `↓` na busca, setas | navega pelos apps |
| botão direito / tecla Menu | abrir, mover, trocar de grupo, remover, adicionar ao grupo |
| `Esc` ou botão `✕` | fecha Configurações → limpa a busca → esconde a Home (ela continua rodando) |
| botão `—` | minimiza para a barra de tarefas (oculto no modo área de trabalho) |
| botão `❐` / duplo clique na faixa do topo | alterna entre ocupar a tela e **modo janela** (com borda, redimensionável) |
| arrastar a faixa do topo (alça no centro) | leva a Home a qualquer monitor: ocupando a tela, ela encolhe sob o cursor e, ao soltar, ocupa o monitor onde caiu; no modo janela, só move |

A Home ocupa o monitor onde está (`janela` diz como ela ocupa a tela). O monitor, o modo janela e o tamanho dele
ficam em `%APPDATA%\WinHome\estado.json` (fora da config) e valem na próxima vez que o WinHome abrir; se aquele
monitor não estiver conectado, ela abre no principal. Apagar o `estado.json` volta ao padrão.

Para **sair** de vez: botão direito no ícone da bandeja → "Sair do WinHome", ou Configurações → Geral → "Sair do WinHome".
