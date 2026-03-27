# Modulo: Fornecedores

## Entidades
- **Pessoa** (dados pessoais/empresariais) — 1:1 com Fornecedor
- **Fornecedor** (PessoaId — sem campos extras)
- **PessoaContato** + **PessoaEndereco** (reutilizados)

## Endpoints
- `GET /api/fornecedores` — lista
- `GET /api/fornecedores/{id}` — detalhe
- `POST /api/fornecedores` — criar
- `PUT /api/fornecedores/{id}` — atualizar
- `DELETE /api/fornecedores/{id}` — excluir
- `GET /api/fornecedores/{id}/log` — historico

## Particularidades
- **PF/PJ toggle**: tipo de pessoa muda os campos visiveis
  - PJ: CNPJ (primeiro campo), Razao Social, Nome Fantasia, Inscricao Estadual
  - PF: CPF, Nome, RG, Data Nascimento
- **Busca automatica CNPJ**: ao digitar 14 digitos, busca via BrasilAPI (fallback ReceitaWS)
- **Endereco nao obrigatorio** (diferente de Colaboradores)
- Validacao prioriza aba Dados (volta para la se campos faltam)

## Permissao
- Codigo: `fornecedores`
