# WinHome

Home personalizada para o Windows 11: abre ao fazer login, mostra relógio e seus apps em painéis
configuráveis e some/volta com `Ctrl+Alt+Space`.

## Usar
1. `.\scripts\executar.ps1` compila e abre.
2. Engrenagem (canto superior direito): Configurações com tudo ao vivo — tema, cores, fundo, painéis
   (exibir/esconder, ordem, largura) e apps (adicionar dos instalados, renomear, reordenar, remover).
   Também dá para editar `%APPDATA%\WinHome\config.json` à mão ([opções](docs/configuracao.md)).
3. Comece a digitar para buscar em todos os apps instalados; `Enter` abre o primeiro.
   Botão direito num app: mover, trocar de grupo, remover.
4. `Esc` (ou `—`) esconde a Home, que continua rodando; `Ctrl+Alt+Space`, o ícone na bandeja ou abrir o app
   de novo trazem de volta. Sair de vez: botão direito no ícone da bandeja → "Sair do WinHome".
5. Para abrir junto com o Windows: Configurações → Geral → "Iniciar com o Windows".

Remover: `.\scripts\desinstalar.ps1`. Plano e fases: [docs/plano.md](docs/plano.md).
