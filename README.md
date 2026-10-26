# WinHome

Home personalizada para o Windows 11: abre ao fazer login, mostra relógio e seus apps em painéis
configuráveis e some/volta com `Ctrl+Alt+Space`.

## Usar
1. `.\scripts\executar.ps1` compila e abre.
2. Edite `%APPDATA%\WinHome\config.json`; a Home atualiza sozinha ao salvar.
3. `Esc` esconde a Home; `Ctrl+Alt+Space` (ou abrir o app de novo) traz de volta.
4. Para abrir junto com o Windows: `"iniciarComWindows": true` na config.

Remover: `.\scripts\desinstalar.ps1`. Plano e fases: [docs/plano.md](docs/plano.md).
