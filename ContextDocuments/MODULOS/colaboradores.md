# Modulo: Colaboradores

## Entidades
- **Pessoa** (dados pessoais) — 1:1 com Colaborador
- **Colaborador** (PessoaId, Cargo, DataAdmissao, Salario, Observacao)
- **PessoaContato** (telefone, email, whatsapp, etc.)
- **PessoaEndereco** (enderecos multiplos)
- **Usuario** (acesso ao sistema, vinculado via ColaboradorId)
- **UsuarioFilialGrupo** (permissoes por filial)

## Endpoints
- `GET /api/colaboradores` — lista
- `GET /api/colaboradores/{id}` — detalhe (com contatos, enderecos, acesso)
- `POST /api/colaboradores` — criar
- `PUT /api/colaboradores/{id}` — atualizar
- `DELETE /api/colaboradores/{id}` — excluir
- `GET /api/colaboradores/{id}/log` — historico

## Abas do formulario
1. **Dados Pessoais**: Nome*, CPF* (mascara), RG, Data Nascimento, Cargo, Data Admissao, Salario (mascara monetaria), Observacao, Ativo
2. **Endereco**: Lista com add/remove, CEP auto-fill, Tipo, Principal
3. **Contato**: Lista com add/remove, Tipo (TELEFONE/CELULAR/EMAIL/WHATSAPP), Valor, Descricao, Principal
4. **Acesso**: Login, Senha, Filial Padrao, Administrador, Sessao Maxima, Inatividade, Grid filial x grupos (multi-select dropdown)

## Regras
- CPF validado (digitos verificadores) e unico
- Login: 6 a 24 caracteres
- Senha: 4 a 12 caracteres
- Um usuario pode pertencer a multiplos grupos por filial
- Sessao: 0 = herda de Configuracoes

## Permissao
- Codigo: `colaboradores`
