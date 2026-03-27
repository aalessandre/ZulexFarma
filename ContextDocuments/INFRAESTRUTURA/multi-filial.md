# Infraestrutura: Multi-filial

## BaseEntity (todas as tabelas)
- `VersaoSync` (long) — incrementa automaticamente a cada alteracao
- `FilialOrigemId` (long?) — preenchido automaticamente com a filial do usuario

## AppDbContext.SaveChangesAsync
- Auto-incrementa VersaoSync em updates
- Seta FilialOrigemId em inserts baseado no JWT

## FilialContexto (servico)
- Extrai FilialId, UsuarioId, IsAdmin do JWT
- Injetavel em qualquer service
- Registrado como Scoped no DI

## SyncControle (tabela)
- Controla estado de sync por (FilialId, Tabela)
- Campos: UltimaVersaoRecebida, UltimaVersaoEnviada, UltimoSync, Status, MensagemErro
- Index unico em (FilialId, Tabela)

## Entidades que NAO herdam BaseEntity
- Configuracao (tem Id proprio sem sync)
- SyncControle (controle, nao dado)
