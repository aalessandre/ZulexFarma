# Infraestrutura: Multi-filial

## Conceito geral
- Cada farmacia roda seu proprio backend + PostgreSQL local
- Railway (nuvem) eh o servidor central passivo
- Sync bidirecional via fila de operacoes (SyncFila)
- Todas as tabelas replicam (exceto controle interno)
- Qualquer filial pode ver dados de qualquer outra

## Identificadores
- **Id** (bigint): tecnico, PK, auto-increment LOCAL. Pode variar entre PCs.
- **Codigo** (varchar): visivel, formato "FilialCodigo.Sequencial" (ex: "2.115950"). Unico global.
- **FilialOrigemId** (bigint): identifica qual farmacia/servidor criou o registro.

### Diferenca entre Id, Codigo e FilialOrigemId
- Id: o banco local gera. Diferente entre PCs. Usado para FKs locais.
- Codigo: o sistema gera no formato "Filial.Seq". Mesmo em todos os PCs. Identifica o registro globalmente.
- FilialOrigemId: automatico, identifica a origem. Usado pelo sync para filtrar o que enviar/receber.

## Escopo das tabelas

### Global
Dados compartilhados, qualquer filial cria/edita:
- Fabricantes, Substancias, GruposPrincipais, GruposProdutos, SubGrupos, Secoes
- Pessoas, Colaboradores, Fornecedores
- UsuariosGrupos, UsuariosGruposPermissao, UsuarioFilialGrupos
- Usuarios, Filiais

### Por Filial (futuro)
Cada filial tem seus registros, visiveis por todas:
- Estoque, Vendas, Caixa, Financeiro

### NAO replicam
- Configuracoes (cada filial define a sua)
- SyncFila, SequenciaLocal (controle interno)
- DicionarioTabelas, DicionarioRevisoes (ferramenta dev)

## BaseEntity
```csharp
public long Id { get; set; }           // PK tecnico local
public string? Codigo { get; set; }    // "FilialCodigo.Sequencial"
public DateTime CriadoEm { get; set; }
public DateTime? AtualizadoEm { get; set; }
public bool Ativo { get; set; }
public long? FilialOrigemId { get; set; }
```

## SaveChangesAsync
- INSERT: gera Codigo automatico, seta FilialOrigemId, registra na SyncFila
- UPDATE: seta AtualizadoEm, registra na SyncFila
- DELETE: registra na SyncFila
- AplicandoSync=true: nao gera Codigo nem registra na fila (usado no PULL)

## Configuracao por filial
```json
{
  "Filial": { "Codigo": 1 }
}
```
Define o numero da filial local. Usado para:
- Gerar Codigo visivel (prefixo)
- Identificar FilialOrigemId nos registros
- Filtrar operacoes no sync (nao puxar as proprias)
