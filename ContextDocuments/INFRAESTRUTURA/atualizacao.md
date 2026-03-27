# Infraestrutura: Atualizacao Automatica

## SistemaController
- `GET /api/sistema/versao` (anonimo) — versao e build
- `GET /api/sistema/info` — info completa (versao, .NET, OS, CPU, RAM, uptime, servicos)
- `GET /api/sistema/atualizacao/verificar` — compara com manifest remoto
- `GET /api/sistema/atualizacao/status` — status do background service

## UpdateBackgroundService
- Verifica periodicamente se ha versao nova
- Config: `Atualizacao:Habilitado`, `Atualizacao:UrlManifest`, `Atualizacao:IntervaloVerificacaoMinutos`
- Propriedades estaticas para consulta pelo controller

## Manifest remoto (formato)
```json
{
  "versao": "1.1.0",
  "build": "2026.04.01",
  "descricao": "Descricao da versao",
  "urlDownload": "https://...",
  "tamanhoMb": 45.2,
  "dataPublicacao": "2026-04-01"
}
```

## Tela de Sistema
- Rota: `/erp/sistema`
- Cards: Versao, Uptime, Maquina, Plataforma
- Painel de atualizacao (disponivel/atualizado)
- Status dos servicos (sync, atualizacao)
