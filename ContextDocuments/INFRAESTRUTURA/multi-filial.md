# Infraestrutura: Multi-filial

## Conceito geral
- Existem DOIS campos diferentes: **FilialOrigemId** (tecnico/sync) e **FilialId** (negocio)
- **FilialOrigemId**: automatico, existe em toda tabela que herda BaseEntity. Identifica de qual PC/servidor o registro foi criado. Invisivel para o usuario. Usado pelo sync.
- **FilialId**: existe APENAS em tabelas onde o registro precisa pertencer a uma filial por regra de negocio (Usuarios, UsuarioFilialGrupos, futuras Estoque/Vendas/Caixa)
- **Tabelas globais** (Fabricantes, Substancias): tem FilialOrigemId (sync) mas NAO tem FilialId (nao pertencem a uma filial)
- **Tabelas por filial** (Configuracoes, Estoque, Vendas): tem FilialOrigemId (sync) E FilialId (negocio)
- **Escrita**: so a filial dona edita seus registros
- **Leitura**: qualquer filial consulta dados de qualquer outra via combo de selecao
- Quase todas as tabelas replicam. Excecoes: SyncControle, DicionarioTabelas/Revisoes

## Escopo das tabelas

### Global (sem FilialId ou FilialId=null)
Dados compartilhados, todos veem igual, qualquer filial pode criar/editar:
- Fabricantes, Substancias, GruposPrincipais, GruposProdutos, SubGrupos, Secoes
- Pessoas, Colaboradores, Fornecedores
- UsuariosGrupos, UsuariosGruposPermissao

### Por Filial (com FilialId obrigatorio)
Cada filial tem seus registros, visiveis por todas via combo:
- Configuracoes, LogsAcao, LogsErro
- Futuros: Estoque, Vendas, Caixa, Financeiro, Manutencao

### NAO replicam
Ficam apenas no banco local de cada PC:
- SyncControle (controle interno do sync)
- DicionarioTabelas, DicionarioRevisoes (ferramenta dev, backup via JSON no Git)

## BaseEntity (todas as tabelas sincronizaveis)
- `VersaoSync` (long) — timestamp milissegundos, monotonicamente crescente
- `FilialOrigemId` (long?) — de qual servidor/farmacia o registro foi criado

## AppDbContext.SaveChangesAsync
- Atribui VersaoSync via ProximaVersaoSync() em Added e Modified
- Seta FilialOrigemId baseado em Sync:FilialLocalId (config) com fallback para JWT
- `SuspenderAutoSync`: quando true, nao altera VersaoSync (usado no PULL local)

## FilialContexto (servico)
- Extrai FilialId, UsuarioId, IsAdmin do JWT
- Injetavel em qualquer service
- Registrado como Scoped no DI

## SyncControle (tabela)
- Controla estado de sync por (FilialId, Tabela)
- Campos: UltimaVersaoRecebida, UltimaVersaoEnviada, UltimoSync, Status, MensagemErro
- Index unico em (FilialId, Tabela)
- NAO replica (cada PC tem o seu)

## Entidades que NAO herdam BaseEntity
- Configuracao (futuramente sera migrada para herdar BaseEntity com FilialId)
- SyncControle (controle interno)
- DicionarioTabela, DicionarioRevisao (ferramenta dev)
