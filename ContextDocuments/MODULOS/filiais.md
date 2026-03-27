# Modulo: Filiais

## Entidade
**Tabela**: `Filiais` (herda BaseEntity)

| Campo | Tipo | Obrigatorio |
|-------|------|-------------|
| NomeFilial (Apelido) | varchar(100) | Sim |
| RazaoSocial | varchar(150) | Sim |
| NomeFantasia | varchar(100) | Sim |
| Cnpj | varchar(18) | Sim, unico |
| InscricaoEstadual | varchar(30) | Nao |
| Cep | varchar(9) | Sim |
| Rua | varchar(200) | Sim |
| Numero | varchar(10) | Sim |
| Bairro | varchar(100) | Sim |
| Cidade | varchar(100) | Sim |
| Uf | varchar(2) | Sim |
| Telefone | varchar(20) | Sim |
| Email | varchar(150) | Sim |

## Endpoints
- `GET /api/filiais` — lista
- `POST /api/filiais` — criar
- `PUT /api/filiais/{id}` — atualizar
- `DELETE /api/filiais/{id}` — excluir (hard delete com fallback soft delete)
- `GET /api/filiais/{id}/log` — historico de auditoria

## Regras
- CNPJ validado (digitos verificadores) e unico
- Exclusao tenta hard delete; se FK impede, faz soft delete (Ativo=false)
- Log de auditoria em todas as operacoes

## Permissao
- Codigo: `filiais`
- Acoes: c (consultar), i (incluir), a (alterar), e (excluir)

## Frontend
- Componente: `frontend/src/app/modules/filiais/`
- Visual: tiles sidebar, card com bordas arredondadas
- Auto-fill CEP via ViaCEP
- Mascaras: CNPJ, Telefone, CEP
