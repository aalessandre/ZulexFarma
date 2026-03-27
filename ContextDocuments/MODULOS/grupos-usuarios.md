# Modulo: Grupos de Usuarios e Permissoes

## Entidades
- **GrupoUsuario** (Nome, Descricao) — tabela `UsuariosGrupos`
- **GrupoPermissao** (GrupoUsuarioId, Bloco, CodigoTela, NomeTela, PodeConsultar, PodeIncluir, PodeAlterar, PodeExcluir) — tabela `UsuariosGruposPermissao`

## Endpoints
- `GET /api/grupos` — lista
- `POST /api/grupos` — criar
- `PUT /api/grupos/{id}` — atualizar
- `DELETE /api/grupos/{id}` — excluir (soft delete)
- `GET /api/grupos/{id}/permissoes` — listar permissoes
- `PUT /api/grupos/{id}/permissoes` — salvar permissoes (delete + insert)

## Permissoes: TreeView
- Tela usa TreeView com 3 niveis: Bloco -> Tela -> Acoes (checkboxes)
- Blocos: Cadastros (2), Manutencao (4)
- Campo de busca filtra telas por nome
- Botoes Expandir/Recolher todos

## Telas cadastradas (telas-sistema.ts)
- Cadastros: filiais, colaboradores, fornecedores, fabricantes, gerenciar-produtos
- Manutencao: grupos, log-geral

## Grupos padrao (seed)
- Administrador, Gerente, Caixa, Vendedor, Estoquista

## Permissao
- Codigo: `grupos`
