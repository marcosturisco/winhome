# Plano do WinHome (26/10/2026)

## Objetivo
Uma Home para o Windows 11: abre ao fazer login, ocupa a tela, mostra painéis configuráveis
(relógio, apps de trabalho, pessoal, projetos, ações rápidas…) e some/volta com uma tecla de atalho.
Não substitui o `explorer.exe`: barra de tarefas, menu Iniciar e área de trabalho continuam iguais.

## Decisões
| Tema | Decisão | Motivo |
|---|---|---|
| Projeto | Repositório próprio (`winhome`), separado do `tvlauncher` | Ferramentas e regras diferentes |
| Interface | WinUI 3 (Windows App SDK) + .NET 10, C# | Visual nativo do Win 11 (Mica), leve |
| Empacotamento | Sem MSIX, Windows App SDK self-contained | Abre qualquer programa, sem instalar runtime |
| Abrir apps | `shell:AppsFolder\<AppID>` (mesmo ID do `Get-StartApps`) ou caminho do `.exe` | Um jeito só para Store e Win32 |
| Iniciar com o Windows | Valor `WinHome` em `HKCU\...\Run` | Sem administrador, reversível |
| Atalho | `Ctrl+Alt+Space` (configurável) | `Win+Space` já troca o idioma do teclado |
| Configuração | `%APPDATA%\WinHome\config.json` com recarga ao vivo | Fácil de mudar sem recompilar |

## Configuração
- **Geral:** nome na saudação, modo da janela (`maximizada` mantém a barra de tarefas; `telaCheia` cobre tudo),
  atalho, iniciar com o Windows, esconder a Home ao abrir um app.
- **Tema:** modo (claro/escuro/sistema), cor de destaque, fundo (`mica`, `micaAlt`, `acrilico`, `cor`, `imagem`),
  fonte, cantos, tamanho dos ícones, opacidade dos painéis, espaçamento e número de colunas da grade.
- **Painéis:** lista em ordem de exibição; cada um com `visivel` (toggle), `titulo` e `largura` (em colunas).
- **Grupos de apps:** cada grupo vira um painel; cada app tem `nome` e `app` (AppID) **ou** `caminho`,
  mais `args`, `pasta` e `icone` opcionais.

## Fases
| Versão | Entrega |
|---|---|
| **v0.1** ✅ | Janela, fundo Mica, `config.json` com recarga ao vivo, painéis relógio e grupos de apps, abrir apps, ícones do shell, atalho global, instância única, iniciar com o Windows |
| **v0.2** ✅ | Tela de Configurações (toggles, temas, adicionar/remover/reordenar apps e painéis), busca ao digitar (grupos + todos os apps instalados), menu de contexto nos apps, `config.json.bak`, ícone na bandeja (Mostrar/Configurações/Sair) e ícone próprio |
| v0.3 | Painéis: projetos recentes, ações rápidas (com confirmação), tarefas, agenda (ICS), dispositivos (TV/box online) |
| v0.4 | Modo "área de trabalho" (Home atrás das janelas, no lugar do papel de parede), com toggle |
| v1.0 | Backup/restauração da config, temas prontos, instalador simples |
