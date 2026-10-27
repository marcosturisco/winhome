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

## Tema (`tema`)
| Opção | Valores | Padrão |
|---|---|---|
| `modo` | `escuro` · `claro` · `sistema` | `escuro` |
| `corDestaque` | `#RRGGBB` ou `#AARRGGBB` (títulos e relógio) | `#4F8CFF` |
| `fundo` | `mica` · `micaAlt` · `acrilico` · `cor` · `imagem` | `mica` |
| `corFundo` | cor usada com `fundo: cor` | `#101418` |
| `imagemFundo` | caminho da imagem com `fundo: imagem` (aceita `%VARIAVEIS%`) | `""` |
| `fonte` | nome de fonte instalada | `Segoe UI Variable` |
| `cantos` | arredondamento dos painéis (px) | `12` |
| `tamanhoIcone` | 16 a 256 | `48` |
| `opacidadePaineis` | 0 (transparente) a 1 (sólido) | `0.6` |
| `espacamento` | distância entre painéis (px) | `16` |
| `colunas` | colunas da grade (1 a 24) | `6` |

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
| `cotacoes.moedas` | códigos em relação ao real: `USD`, `EUR`, `GBP`, `JPY`, `CAD`, `AUD`, `CHF`, `CNY`, `ARS`, `BTC`, `ETH` | `["USD", "EUR"]` |

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

## Painéis (`paineis`)
Lista em ordem de exibição. Cada item: `id`, `visivel` (true/false), `largura` (colunas) e `titulo` (opcional).
- `id: "boasVindas"`: saudação, hora e data.
- `id: "mensagem"`: mensagem do dia.
- `id: "curiosidade"`: curiosidades do dia.
- `id: "projetos"`: projetos recentes.
- `id: "tarefas"`: tarefas.
- Qualquer outro `id`: grupo de apps com o mesmo nome em `grupos`.
- Em Configurações → Painéis, "+ Adicionar painel" cria um grupo de apps ou um painel especial que ainda não esteja na Home.

## Grupos de apps (`grupos`)
Cada grupo é uma lista de apps:
| Campo | Uso |
|---|---|
| `nome` | texto embaixo do ícone |
| `app` | AppID do menu Iniciar (veja com `Get-StartApps` no PowerShell) — vale para apps da Store e comuns |
| `caminho` | alternativa ao `app`: `.exe`, atalho, pasta ou documento (aceita `%VARIAVEIS%`) |
| `args`, `pasta` | argumentos e pasta de trabalho (só com `caminho`) |
| `icone` | imagem própria para o ícone (opcional) |

## Teclas
| Tecla | Ação |
|---|---|
| `Ctrl+Alt+Space` (configurável) | mostra/esconde a Home |
| digitar | busca nos grupos e em todos os apps instalados |
| `Enter` na busca | abre o primeiro resultado |
| `↓` na busca, setas | navega pelos apps |
| botão direito / tecla Menu | abrir, mover, trocar de grupo, remover, adicionar ao grupo |
| `Esc` ou botão `✕` | fecha Configurações → limpa a busca → esconde a Home (ela continua rodando) |
| botão `—` | minimiza para a barra de tarefas |
| botão `❐` / duplo clique na faixa do topo | alterna entre ocupar a tela e **modo janela** (com borda, redimensionável) |
| arrastar a faixa do topo (alça no centro) | leva a Home a qualquer monitor: ocupando a tela, ela encolhe sob o cursor e, ao soltar, ocupa o monitor onde caiu; no modo janela, só move |

A Home ocupa o monitor onde está (`janela` diz como ela ocupa a tela). O monitor, o modo janela e o tamanho dele
ficam em `%APPDATA%\WinHome\estado.json` (fora da config) e valem na próxima vez que o WinHome abrir; se aquele
monitor não estiver conectado, ela abre no principal. Apagar o `estado.json` volta ao padrão.

Para **sair** de vez: botão direito no ícone da bandeja → "Sair do WinHome", ou Configurações → Geral → "Sair do WinHome".
