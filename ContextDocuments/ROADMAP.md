# Roadmap

## Implementado
- [x] Filiais (CRUD completo)
- [x] Colaboradores (CRUD + acesso + multi-grupo)
- [x] Fornecedores (PF/PJ + busca CNPJ)
- [x] Fabricantes (CRUD simples)
- [x] Substancias (CRUD simples — PC2)
- [x] Grupos de Usuarios + Permissoes (TreeView)
- [x] Gerenciar Produtos (4 classificacoes: Grupo Principal, Grupo, Sub Grupo, Secao)
- [x] Log de Auditoria (filtros, expansivel, campos alterados em laranja)
- [x] Configuracoes do Sistema (sessao, nome)
- [x] Sistema de permissoes enforcado
- [x] Liberacao por senha de supervisor
- [x] Usuario SISTEMA (senha rotativa)
- [x] Controle de sessao (timeout + inatividade)
- [x] Modal global (aviso, confirmacao, permissao)
- [x] Dashboard estilo launcher (tiles, fundo configuravel)
- [x] Infraestrutura multi-filial (VersaoSync, FilialOrigemId)
- [x] Infraestrutura de sync (API + background service + tela)
- [x] SyncBackgroundService completo (push/pull automatico com Railway)
- [x] Replicacao testada entre 2 PCs (PC1↔Railway↔PC2)
- [x] Infraestrutura de atualizacao (versionamento + tela sistema)
- [x] Deploy (Railway + GitHub)
- [x] Enter -> Tab em todas as telas
- [x] Login com filial padrao automatica
- [x] Alterar senha propria
- [x] Icones SVG inline em todos os modulos ERP
- [x] Botao Procurar recarrega dados em todos os modulos

## Em andamento
- [ ] Lapidar processo de replicacao e tela de sync

## Proximos passos
- [ ] Cadastro de Produtos (aba principal)
- [ ] Cadastro de Familia (aba)
- [ ] Cadastro de Clientes
- [ ] Tela de Vendas (PDV)
- [ ] Controle de Estoque
- [ ] Caixa
- [ ] Financeiro
- [ ] Relatorios
- [ ] Testar sync multi-filial com 2+ PCs
- [ ] Auto-update funcional (download + apply)
