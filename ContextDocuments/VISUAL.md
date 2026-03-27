# Padroes Visuais

## Estilo geral
- Fonte: **Poppins** (sans-serif)
- Fundo: **branco** (#ffffff)
- Sem linhas divisorias entre secoes
- Cards com `border-radius: 16px`, `border: 1px solid #e8ecf1`, `box-shadow` sutil
- Locale: `pt-BR` (datas dd/MM/yyyy)

## Dashboard
- Estilo **launcher** com tiles coloridos
- Fundo escuro configuravel (padrao #1a1a2e)
- Tiles: 170x105px, border-radius 10px, sigla grande + label
- Cores por bloco: Movimento (ciano), Cadastros (laranja), Relatorios (roxo), Manutencao (amarelo)
- Nome ZULEXFARMA: Poppins 32px, cor azulada #5a7fa8, sem negrito

## Telas internas
- **Sidebar**: tiles coloridos (180px), fundo transparente
  - Procurar (azul), Adicionar (verde/teal), Editar (azul claro), Sair (vermelho)
- **Topbar**: fundo branco, 70px, nome da tela em azulado, avatar com icone
- **Grid**: dentro de card arredondado, colunas configuraveis/redimensionaveis
- **Formulario**: dentro de card arredondado, secoes com titulo azul
- **Toolbar**: fundo branco, sem bordas

## Configuracoes (engrenagem)
- Modo: claro/escuro
- Tamanho da fonte: normal/grande/muito grande/extra grande
- Cor de destaque: 6 presets + color picker
- Fundo do dashboard: 5 presets + color picker

## Modais (ModalService global)
- Tipos: aviso (laranja), sucesso (verde), erro (vermelho), confirmacao (azul), permissao (laranja + login/senha)
- Icone circular, titulo bold, mensagem, botoes
- Animacao scale-in

## Icones
- **Padrao**: SVG inline em todos os modulos do ERP
- Nao usar Unicode characters (×, &#9432;, &#9650;, etc.) — sempre SVG
- Nao usar icon fonts (Material Icons, Font Awesome) nas telas do ERP
- SVGs: `stroke="currentColor"` para herdar cor do contexto
- Tamanhos comuns: 10px (sort/seta), 12px (check), 14px (fechar/info/warning)

## Enter -> Tab
- Diretiva `EnterTabDirective` em todos os formularios
- Enter pula para proximo campo focusavel
