# Padrão de Tela CRUD — ERP ZulexPharma

Referência: `colaboradores.component` (HTML + SCSS + TS)
Quando solicitado "ajustar conforme padrão", aplicar TODOS os itens abaixo.

---

## 1. ESTRUTURA HTML

```
tela-erp
├── aside.tela-sidebar
│   ├── sidebar-item.tile-procurar   (Lupa + "Procurar" + "Consulta")
│   ├── sidebar-item.tile-adicionar  (+ "Adicionar" + "Novo cadastro")
│   ├── sidebar-item.tile-editar     (Lápis + "Editar" + "Alterar registro")
│   ├── sidebar-item.tile-sair       (Porta + "Sair" + "Fechar tela")
│   └── sidebar-aba[]                (abas abertas com #codigo, nome, status)
├── div.tela-main
│   ├── modo=lista → lista-header + grid-wrapper + grid-rodape
│   └── modo=form  → form-wrapper + tela-toolbar
└── modal-overlay (histórico)
```

## 2. SIDEBAR

### Ícones SVG nos botões
Dentro de `.sidebar-item-header`, SVGs com `width:15 height:15 stroke-width:2.5`.
CSS força tamanho via `.sidebar-item-header svg { width: 20px; height: 20px; }`.

### Cores dos tiles
```scss
.tile-procurar  { background: linear-gradient(135deg, #2471a3, #2e86c1); }
.tile-adicionar { background: linear-gradient(135deg, #1abc9c, #16a085); }
.tile-editar    { background: linear-gradient(135deg, #2980b9, #3498db); }
.tile-sair      { background: linear-gradient(135deg, #c0392b, #e74c3c); }
```

### Abas laterais
- NOVO_ID = `-1` para novos registros
- Ícone `+` para novo, lápis para edição
- Código: `aba.colaborador.codigo || aba.colaborador.id`
- Status: `Criando | Novo | Editando | Consultando`
- Indicador dirty: `●`
- Fundo aba: `#e8ecf1`, borda, botão X para fechar

## 3. MODO LISTA (GRID)

### Header
```html
<div class="lista-header">
  <div class="lista-filtro">
    <label>PROCURAR POR</label>
    <input class="input-busca" placeholder="Digite ao menos 2 caracteres..." />
  </div>
  <div class="lista-filtro">
    <label>SITUAÇÃO</label>
    <select> Ativos | Inativos | Todos </select>
  </div>
  <button class="btn-colunas"> Colunas </button>  <!-- col-picker -->
</div>
```

### Grid (classe global `erp-grid`)
- Zebra stripes: `tbody tr:nth-child(even) { background: rgba(0,0,0,0.02) }`
- Divisores verticais: `th, td { border-right: 1px solid var(--erp-border) }`
- Hover: `var(--erp-row-hover)`
- Selecionado: `var(--erp-row-selected)` + chevron SVG na td-arrow
- Colunas redimensionáveis: `.resize-handle` em cada th
- Colunas reordenáveis: drag & drop
- Rodapé: `Registro(s): <strong>N</strong>`

### Mascaramento no grid
- CPF: `000.000.000-00` (via getCellValue)
- CNPJ: `00.000.000/0000-00`
- Telefone: `(00) 00000-0000`
- Coluna `codigo` (não `id`)

## 4. MODO FORMULÁRIO

### Título dinâmico
```
modoEdicao && isDirty  → "Editando [Entidade]"
modoEdicao && !isDirty → "Consulta [Entidade]"
!modoEdicao && isDirty → "Criando [Entidade]"
!modoEdicao && !isDirty→ "Novo [Entidade]"
```

### Botão X (fechar)
```html
<button class="btn-fechar-form"> <!-- background: var(--erp-bloco-bg) -->
  <svg>X</svg>
</button>
```

### Blocos visuais (form-bloco)
```html
<div class="form-bloco">
  <label style="font-size:var(--erp-fs-sm);font-weight:700;color:var(--erp-primary);
    letter-spacing:0.7px;display:block;margin-bottom:8px">TITULO DO BLOCO</label>
  <!-- conteúdo -->
</div>
```
CSS: `border: 1px solid var(--erp-border); border-radius: 10px; padding: 16px; margin-bottom: 12px; background: var(--erp-bloco-bg);`

### Layout de campos (form-grid)
```scss
.form-grid {
  display: flex; flex-wrap: wrap;
  row-gap: 14px; column-gap: 24px;
  margin-bottom: 10px;
}
```

### Tamanhos de campo
| Classe/Style | Uso | Exemplo |
|---|---|---|
| `style="width:160px"` | Campos curtos | Código, Data, CPF, RG, UF, CEP, Gênero |
| `field-2` (flex:1, min:240px) | Campos médios | Apelido, Bairro, Cidade |
| `field-3` (flex:2, min:320px) | Campos longos | Nome, Razão Social, Rua |
| `field-full` (100%) | Campos inteiros | Login, Senha (dentro de layouts restritos) |

### Campos obrigatórios
Label com `*`: `<label>NOME *</label>`

### Texto maiúsculo
CSS: campos usam font padrão (sem text-transform forçado global).
JS: `updateForm()` aplica `.toUpperCase()` exceto datas.

### Campos alterados
```html
[class.field-alterado]="campoAlterado('campo')"
```
CSS: `border-color: #f0ad4e; background: #fffdf5;`

### Campos inválidos
```html
[class.field-invalido]="erroCampo('campo')"
@if (erroCampo('campo')) { <span class="field-erro">{{ erroCampo('campo') }}</span> }
```

### Ordem padrão de campos (quando aplicável)
1. **Linha 1**: Código (disabled) | Data Cadastro (disabled) | campos curtos específicos
2. **Linha 2**: Documento (CPF/CNPJ) antes do Nome → documento SEMPRE antes do nome
3. **Linha 3**: Campos secundários (RG, Apelido, datas, valores)

### Campo monetário (R$)
```html
<div class="input-prefixo-wrap">
  <span class="input-prefixo-inner">R$</span>  <!-- pointer-events:none, position:absolute -->
  <input style="padding-left:32px" />
</div>
```

## 5. TOGGLES E CHECKBOXES

### Toggle switch (erp-toggle) — para campos booleanos de formulário
```html
<label class="erp-toggle">
  <input type="checkbox" [checked]="..." (change)="..." />
  <span class="erp-toggle-track"><span class="erp-toggle-thumb"></span></span>
  <span class="erp-toggle-label">Texto</span>
</label>
```
Track: 36x20px, azul quando checked. Thumb: 16x16px branco.

### Toggle Ativo — cor especial
```html
<label class="erp-toggle erp-toggle-ativo">
```
- Checked: verde `#27ae60`
- Unchecked: vermelho `#c0392b`
- Posicionado à direita com `margin-left:auto` quando na linha de toggles

### Checkbox estilizado (erp-check-card) — para "Principal" em endereço/contato
```html
<label class="erp-check-card">
  <input type="checkbox" />
  <span class="erp-check-box"><svg check-branco /></span>
  <span class="erp-check-text">Principal</span>
</label>
```
Sem borda de card, apenas checkbox azul arredondado + texto.

### Onde usar cada um
- **Toggle**: Ativo, permissões, flags on/off (Bloqueado, Permite Fidelidade, etc.)
- **Checkbox card**: "Principal" em endereço/contato
- **Checkbox nativo**: Seleção em grids, col-picker, multiselect

## 6. ACCORDIONS (Endereços, Contatos, Observação)

- Iniciam fechados
- Background: `var(--erp-bloco-bg)`
- Header: padding 8px, chevron SVG rotação 180deg quando aberto
- Body: padding 8px 16px
- Cada item: `.item-card` com header (título + checkbox Principal + botão remover)
- Botão `+ Adicionar` no último item, alinhado à direita
- **Validação ao salvar**: endereços/contatos existentes são validados (campos obrigatórios em vermelho)
- **Descarte automático**: após validação, `limparVazios()` remove itens totalmente vazios
- Endereço vazio = sem CEP, Rua e Número. Contato vazio = sem Valor.
- **Ao remover item (X)**: limpa mensagem de erro, limpa erros dos campos, e verifica `isDirty` comparando com `formOriginal` via `verificarDirty()` — se voltou ao estado original, esconde Salvar/Cancelar

### Tipo de Endereço
Opções: `CASA | ENTREGA | COBRANÇA | OUTRO` (não usar PRINCIPAL — "Principal" é checkbox)

### CEP
- Busca automática ao completar 8 dígitos
- Botão lupa manual (`.btn-busca-cep`)
- Busca no blur
- Preenche: Rua, Bairro, Cidade, UF

## 7. TOOLBAR INFERIOR (tela-toolbar)

### Botões e cores
```
Salvar    → .tb-salvar   { background: #27ae60; color: #fff; }  // Verde — só aparece se isDirty
Cancelar  → .tb-cancelar { background: #e67e22; color: #fff; }  // Laranja — só aparece se isDirty
Fechar    → .tb-fechar   { background: #546e7a; color: #fff; }  // Cinza-azulado — sempre visível
─── separador ───
Histórico → .tb-log      { background: #2980b9; color: #fff; }  // Azul — só se modoEdicao
Excluir   → .tb-excluir  { background: #c0392b; color: #fff; }  // Vermelho — só se modoEdicao
```

### Estilo dos botões
```scss
.tb-btn {
  gap: 8px; padding: 0 20px; height: 38px;
  border: none; border-radius: 8px; font-size: var(--erp-fs-sm); font-weight: 600;
  svg { width: 18px; height: 18px; }
  &:hover { filter: brightness(1.1); transform: translateY(-1px); box-shadow: 0 2px 8px rgba(0,0,0,0.15); }
}
```

### Ordem
`[Salvar] [Cancelar] [Fechar] ──separador── [Histórico] [Excluir]`

## 8. ATALHOS DE TECLADO

| Tecla | Ação |
|---|---|
| `Ctrl+S` | Salvar (se isDirty) |
| `Escape` | Se form: cancelar edição / fechar. Se lista: fechar tela. Marca `(e as any).__handled = true` para hierarquia com shell |
| `F2` | Editar registro selecionado |
| `Enter` | No grid: abrir registro. Na busca: confirmar busca |
| `↑↓` | Navegar grid |
| `Home` | Ir para dashboard (tratado no shell) |
| `Ctrl+K` | Busca global (tratado no shell) |

### Hierarquia ESC
Componentes filhos marcam `(e as any).__handled = true`. Shell usa `setTimeout` para verificar antes de fechar tab.

## 9. PERSISTÊNCIA (sessionStorage)

- Auto-registro da tab no `ngOnInit` via `tabService.registrarTab()`
- `beforeClose` handler para confirmação se dirty
- `beforeunload` para proteger contra fechar navegador
- Abas salvas em sessionStorage com formato completo (form + isDirty)
- NOVO_ID persistido entre telas

## 10. CSS SCSS — BLOCOS ESSENCIAIS

Cada componente CRUD deve ter no SCSS:

```scss
// Sidebar
.sidebar-item-header { display: flex; align-items: center; gap: 8px; svg { width: 20px; height: 20px; } }

// Form grid
.form-grid { display: flex; flex-wrap: wrap; row-gap: 14px; column-gap: 24px; margin-bottom: 10px; }

// Field sizes
.field { display: flex; flex-direction: column; gap: 4px; min-width: 140px; }
.field-2 { min-width: 240px; flex: 1; }
.field-3 { min-width: 320px; flex: 2; }
.field-full { min-width: 100%; flex: 1; }

// Bloco visual
.form-bloco { border: 1px solid var(--erp-border); border-radius: 10px; padding: 16px; margin-bottom: 12px; background: var(--erp-bloco-bg); }

// Botão X fechar
.btn-fechar-form { background: var(--erp-bloco-bg); border: 1px solid var(--erp-border); border-radius: 6px; &:hover { background: #fde8e8; color: #c0392b; } }

// Toolbar
.tb-btn { border-radius: 8px; height: 38px; font-size: var(--erp-fs-sm); svg { width: 18px; height: 18px; } }
.tb-salvar   { background: #27ae60; color: #fff; }
.tb-cancelar { background: #e67e22; color: #fff; }
.tb-fechar   { background: #546e7a; color: #fff; }
.tb-log      { background: #2980b9; color: #fff; }
.tb-excluir  { background: #c0392b; color: #fff; }
```

## 11. ESTILOS GLOBAIS (styles.scss)

Já disponíveis em todas as telas, NÃO duplicar no SCSS do componente:

- `--erp-bloco-bg`: `#f5f6f8` (light) / `#232b3e` (dark)
- `.erp-toggle` / `.erp-toggle-ativo`: toggle switch global
- `.erp-check-card`: checkbox estilizado
- `.erp-grid`: zebra + divisores + hover + selecionado

## 12. FLUXOS COMPORTAMENTAIS

### Ao criar novo registro
1. Adiciona aba com NOVO_ID=-1 na sidebar
2. Abre formulário vazio com Código="NOVO" e Data=hoje
3. Ao salvar: remove aba -1, cria aba com ID real, recarrega lista

### Ao editar
1. Busca detalhe via GET /api/[entidade]/{id}
2. Abre aba com dados completos
3. `campoAlterado()` compara form vs original

### Ao salvar
1. `verificarPermissao()` → `validar()` → `limparVazios()` → monta payload → POST/PUT
2. `validar()` valida TODOS os endereços/contatos existentes (campos obrigatórios em vermelho)
3. Se tem erro em endereço: expande o accordion automaticamente e foca no primeiro campo inválido
4. `limparVazios()` roda DEPOIS da validação — remove itens totalmente vazios antes de enviar

### Ao remover endereço/contato (botão X)
1. Remove o item do array
2. Limpa `erro()` (mensagem do topo) e `errosCampos()` (bordas vermelhas)
3. Chama `verificarDirty()` — compara form atual com `formOriginal` via JSON.stringify
4. Se voltou ao estado original: `isDirty = false` → esconde botões Salvar/Cancelar

### verificarDirty()
```typescript
private verificarDirty() {
  if (!this.formOriginal) { this.isDirty.set(true); return; }
  const atual = JSON.stringify(this.[entidade]Form());
  const original = JSON.stringify(this.formOriginal);
  this.isDirty.set(atual !== original);
}
```
Usado em `removerEndereco()` e `removerContato()` no lugar de `marcarDirty()`.

### Ao fechar
1. Se dirty → modal confirmação
2. Remove aba, volta para lista

### Campo documento (CPF/CNPJ)
- Documento SEMPRE vem antes do Nome na ordem dos campos
- Auto-busca pessoa existente no blur (para vincular dados)
