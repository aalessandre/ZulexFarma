# Infraestrutura: Autenticacao e Permissoes

## JWT
- Claims: NameIdentifier, Name, nome, filialId, grupoId, isAdmin, permissoes, sessaoMaxima, inatividade
- Claim `permissoes`: JSON `{"filiais":"ciae","colaboradores":"ci"}`
- Letras: c=consultar, i=incluir, a=alterar, e=excluir
- Expiracao: configuravel (padrao 8h)

## Permissoes
- Atributo `[Permissao("tela", "acao")]` nos controllers
- Filtro `PermissaoFilter` verifica claim `permissoes` do JWT
- Admin (isAdmin=True) bypassa tudo
- Token de liberacao (`X-Liberacao` header) bypassa permissao por 60 segundos

## Liberacao por senha
- `POST /api/auth/liberar` — valida credenciais do supervisor
- Verifica se supervisor tem a permissao requerida
- Gera token UUID (LiberacaoCache, 60 segundos)
- Registra no LogAcao com UsuarioLiberouId
- Frontend: modal com login/senha do supervisor

## Usuario SISTEMA
- Login: SISTEMA (case-insensitive)
- Senha: `SHA256(YYYYMMDD + SistemaKey)[0..8]` em hex
- Endpoint: `GET /api/auth/senha-sistema?key={SistemaApiKey}`
- Acesso total, sem restricoes
- Nao precisa existir no banco

## Controle de sessao
- Tempo maximo: herda de Configuracoes, override por usuario
- Inatividade: monitora mousemove/keydown/click
- Aviso 5 min antes de expirar
- Logout limpa: localStorage, sessionStorage, tabs, timers

## Login flow
1. Usuario digita login -> busca filial padrao (GET /api/auth/filial-usuario/{login})
2. Digita senha -> POST /api/auth/login
3. Backend agrega permissoes de todos os grupos (via UsuarioFilialGrupo)
4. Retorna JWT com claims + lista de filiais de acesso
5. Frontend extrai permissoes e tempos de sessao do JWT
