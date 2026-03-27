# Infraestrutura: Deploy

## Railway
- Projeto: grateful-radiance
- Backend: `zulexfarma-production.up.railway.app` (Dockerfile em backend/)
- Frontend: `giving-art-production-6f68.up.railway.app` (Dockerfile em frontend/)
- PostgreSQL: Railway managed

## Dockerfiles
- Backend: `backend/Dockerfile` — multistage (sdk -> aspnet)
- Frontend: `frontend/Dockerfile` — multistage (node -> nginx)

## Variaveis do backend (Railway)
- ConnectionStrings__DefaultConnection
- JwtSettings__SecretKey, Issuer, Audience, ExpiracaoHoras
- ASPNETCORE_URLS
- CorsOrigins__0

## Deploy automatico
- Push para `main` -> Railway redeploya ambos os servicos
- Branches `dev-pc1` e `dev-pc2` nao trigam deploy

## Frontend environment.prod.ts
- apiUrl: `https://zulexfarma-production.up.railway.app/api`
