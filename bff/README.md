# BFF Odontológico (NestJS · Hexagonal · Keycloak RBAC)

Única porta de entrada HTTP para os clientes (web/mobile). Autentica via JWT do
Keycloak (JWKS/RS256), autoriza por RBAC (`employ` | `dentist` | `pacient`) e
orquestra o **Dentist Service**. Spec em `docs/specs/spec01.md`, tarefas em
`docs/tasks/task01.md`.

## Como subir

```bash
cp .env.example .env   # ajuste KEYCLOAK_* e DENTIST_SERVICE_BASE_URL
pnpm install
pnpm run start:dev     # http://localhost:3000 | Swagger em /docs (se SWAGGER_ENABLED=true)
```

Via Docker:

```bash
docker build -t bff .
docker run -p 3000:3000 --env-file .env bff
# ou com mock do downstream:
docker compose -f docker-compose.dev.yml up
```

`GET /health` é público (liveness).

## Variáveis de ambiente

Ver `.env.example` (todas as variáveis do 10 do spec). Obrigatórias:
`KEYCLOAK_ISSUER`, `KEYCLOAK_JWKS_URI`, `KEYCLOAK_CLIENT_ID`,
`DENTIST_SERVICE_BASE_URL`. O boot **falha** com env inválida, listando o problema.

## Como obter um token de teste

1. No Keycloak, crie o realm/cliente `bff-clinica` com uma role de realm
   `dentist` (valores exatos: `employ`, `dentist`, `pacient` — case-sensitive).
2. Crie um usuário de teste com a role `dentist`.
3. Obtenha o token (Direct Access Grants, apenas dev):
   ```bash
   curl -X POST "$KEYCLOAK_ISSUER/protocol/openid-connect/token" \
     -d "grant_type=password&client_id=bff-clinica&username=dentista1&password=senha"
   ```
4. Chame o BFF:
   ```bash
   curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/dentist-services
   ```

Nos testes automatizados um JWKS local + `signToken` é usado
(`test/support/jwt-test-kit.ts`) — nenhum Keycloak real é necessário.

## Matriz RBAC

| Recurso                         |          `employ`          | `dentist` | `pacient` |
| ------------------------------- | :------------------------: | :-------: | :-------: |
| `/api/dentist-services` (todas) |            403             |    ✅     |    403    |
| `/health`                       |          público           |  público  |  público  |
| `/docs`                         | conforme `SWAGGER_ENABLED` |           |           |

Sem token/token inválido → `401`; sem a role → `403`. Rotas sem
`@Public()`/`@Roles()` negam por padrão (fail-closed). O cliente nunca envia
`X-Dentist-Id`/`dentistId`: o BFF injeta o `sub` do token.

## Scripts

| Comando                        | O quê                                        |
| ------------------------------ | -------------------------------------------- |
| `pnpm run build` / `start:dev` | build / dev                                  |
| `pnpm run lint`                | ESLint + Prettier                            |
| `pnpm run arch:check`          | dependency-cruiser (hexagonal)               |
| `pnpm run test:unit`           | testes unitários (`src/**/*.spec.ts`)        |
| `pnpm run test:int`            | integração (JWKS local + HTTP local)         |
| `pnpm run test:e2e`            | E2E (pulado nesta entrega — sem specs)       |
| `pnpm run test:cov`            | cobertura (metas §12.10 no `jest.config.js`) |

Testes E2E (`T-07.21–T-07.25`) foram **pulados nesta entrega** a infra (`configureApp`, `jwt-test-kit`, `downstream-mock`) está
pronta para reativá-los.

## Como adicionar um novo módulo (ex.: `pacient-services`)

1. Crie `src/modules/<nome>/{domain,application,infrastructure}` no mesmo molde.
2. Declare a porta OUT e os use cases (`UseCase<In,Out>`).
3. Implemente o gateway com `DownstreamHttpModule.forClient(TOKEN)` (uma linha).
4. Anote o controller com `@Auth(Role.X)` — nada muda em `auth/` ou `shared/`
   (exceto novos `DomainError`s no mapa do filtro).
