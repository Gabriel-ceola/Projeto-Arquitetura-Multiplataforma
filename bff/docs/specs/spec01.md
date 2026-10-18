# SPEC — BFF Odontológico (NestJS · Hexagonal · Keycloak RBAC)

> Versão: 1.0 · Status: Rascunho para implementação
> Documento companheiro: `tasks.md`

---

## 1. Visão geral

O BFF (Backend for Frontend) é a única porta de entrada HTTP para os clientes (web/mobile). Ele:

1. **Autentica** requisições validando o JWT emitido pelo **Keycloak** via **JWKS** (assinatura RS256).
2. **Autoriza** por **RBAC** usando as roles `employ`, `dentist` e `patient`.
3. **Orquestra** chamadas a serviços de domínio downstream. Hoje existe apenas o **Dentist Service** (contrato em `openapi.yml`).
4. **Traduz** identidade do token para o contrato do serviço downstream (ex.: `sub` → header `X-Dentist-Id`), de modo que o cliente nunca informe sua própria identidade.

### 1.1 Objetivos

- Arquitetura **hexagonal** (ports & adapters) com dependências apontando sempre para dentro.
- **DRY**: autenticação, autorização, tratamento de erros, cliente HTTP e DTOs/mapeamentos definidos uma única vez e reutilizados por todos os módulos atuais e futuros.
- Adicionar um novo serviço downstream (ex.: pacientes) deve exigir **apenas** um novo módulo, sem tocar no módulo de auth nem na infraestrutura compartilhada.

### 1.2 Fora de escopo (nesta versão)

- Emissão/refresh de tokens (responsabilidade do Keycloak e do cliente).
- Persistência própria (o BFF é stateless).
- Módulos de `patient` e `employ` (a arquitetura os prevê, mas não são implementados).
- Cache de respostas downstream.

---

## 2. Stack

| Item | Escolha |
|---|---|
| Runtime | Node.js 20 LTS+ |
| Framework | NestJS 10+ (Express adapter) |
| Linguagem | TypeScript (`strict: true`) |
| Validação de JWT/JWKS | `jose` (`createRemoteJWKSet` + `jwtVerify`) |
| Config | `@nestjs/config` + `zod` (falha no boot se env inválida) |
| HTTP downstream | `fetch` nativo (undici) com `AbortSignal.timeout` |
| Validação de DTO | `class-validator` + `class-transformer` |
| Docs | `@nestjs/swagger` |
| Segurança HTTP | `helmet`, CORS configurável, `@nestjs/throttler` |
| Logs | `nestjs-pino` (JSON, com `correlationId`) |
| Health | `@nestjs/terminus` |
| Testes | Jest + Supertest |
| Enforcement de arquitetura | `dependency-cruiser` (ou `eslint-plugin-boundaries`) |

---

## 3. Arquitetura hexagonal

### 3.1 Camadas e regra de dependência

```
        ┌──────────────────────────────────────────────────────┐
        │                    infrastructure                    │
        │  (adapters IN: controllers/guards · adapters OUT:    │
        │   http gateways, jose verifier, config)              │
        │        ┌────────────────────────────────────┐        │
        │        │            application             │        │
        │        │   use cases + ports (in / out)     │        │
        │        │      ┌──────────────────────┐      │        │
        │        │      │        domain        │      │        │
        │        │      │ entities · enums ·   │      │        │
        │        │      │ errors · value objs  │      │        │
        │        │      └──────────────────────┘      │        │
        │        └────────────────────────────────────┘        │
        └──────────────────────────────────────────────────────┘
                 dependências apontam SEMPRE para dentro
```

| Camada | Pode importar | Não pode importar |
|---|---|---|
| `domain` | apenas TypeScript puro e `shared/domain` | NestJS, `jose`, `fetch`, DTOs, qualquer `infrastructure` |
| `application` | `domain`, `shared/application` | NestJS HTTP (Request/Response), `jose`, clientes HTTP concretos |
| `infrastructure` | `application`, `domain`, frameworks e libs | — |

> Exceção pragmática: decorators `@Injectable()` e `@Inject()` do NestJS são permitidos em `application` (apenas para DI). Nenhum outro símbolo do Nest.

Essa regra deve ser **verificada automaticamente** (ver T-01.5 em `tasks.md`).

### 3.2 Estrutura de pastas

```
src/
├── main.ts
├── app.module.ts
│
├── shared/                                  # Shared Kernel (DRY)
│   ├── domain/
│   │   ├── domain-error.ts                  # classe base: code + message
│   │   └── common-errors.ts                 # Validation, Unavailable, Timeout...
│   ├── application/
│   │   └── use-case.ts                      # interface UseCase<In, Out>
│   ├── config/
│   │   ├── env.schema.ts                    # schema zod
│   │   └── app-config.module.ts
│   └── infrastructure/
│       ├── http/
│       │   ├── downstream-http-client.ts    # cliente fetch reutilizável (timeout, headers, erros)
│       │   ├── downstream-http.module.ts
│       │   ├── domain-exception.filter.ts   # DomainError -> HTTP (único ponto)
│       │   ├── correlation-id.middleware.ts
│       │   └── error-response.dto.ts        # { message }
│       └── constants/
│           └── headers.ts                   # X_DENTIST_ID, X_CORRELATION_ID...
│
└── modules/
    ├── auth/
    │   ├── domain/
    │   │   ├── role.enum.ts                 # employ | dentist | patient
    │   │   ├── authenticated-user.ts
    │   │   └── auth-errors.ts               # InvalidToken, InsufficientRole
    │   ├── application/
    │   │   ├── ports/
    │   │   │   └── token-verifier.port.ts   # TOKEN_VERIFIER
    │   │   ├── role-extractor.ts            # função pura: claims -> Role[]
    │   │   └── authenticate.use-case.ts     # Bearer -> AuthenticatedUser
    │   └── infrastructure/
    │       ├── jose-jwks-token-verifier.adapter.ts
    │       ├── jwt-auth.guard.ts            # APP_GUARD global
    │       ├── roles.guard.ts               # APP_GUARD global
    │       ├── decorators/
    │       │   ├── public.decorator.ts
    │       │   ├── roles.decorator.ts
    │       │   ├── auth.decorator.ts        # @Auth(...roles) composto
    │       │   └── current-user.decorator.ts
    │       └── auth.module.ts
    │
    └── dentist-services/
        ├── domain/
        │   ├── dentist-service.ts           # entidade/tipo
        │   ├── dentist-service-status.enum.ts
        │   └── dentist-service-errors.ts    # NotFound, Forbidden
        ├── application/
        │   ├── ports/
        │   │   └── dentist-services.gateway.ts   # porta OUT (DENTIST_SERVICES_GATEWAY)
        │   └── use-cases/
        │       ├── list-dentist-services.use-case.ts
        │       ├── get-dentist-service-by-id.use-case.ts
        │       ├── create-dentist-service.use-case.ts
        │       ├── update-dentist-service.use-case.ts
        │       └── delete-dentist-service.use-case.ts
        └── infrastructure/
            ├── http/                        # adapter IN
            │   ├── dentist-services.controller.ts
            │   ├── dto/
            │   │   ├── create-dentist-service.dto.ts
            │   │   ├── update-dentist-service.dto.ts
            │   │   └── dentist-service.response.dto.ts
            │   └── dentist-service.http-mapper.ts
            ├── gateways/                    # adapter OUT
            │   ├── http-dentist-services.gateway.ts
            │   └── downstream-dentist-service.mapper.ts
            └── dentist-services.module.ts
```

---

## 4. Princípios DRY aplicados

| Duplicação a evitar | Solução única |
|---|---|
| Validar JWT em cada controller | `JwtAuthGuard` global (`APP_GUARD`), **fail-closed**: toda rota exige token, exceto as marcadas com `@Public()` |
| Checar role em cada handler | `RolesGuard` global lendo metadata do `@Roles()` |
| Repetir `@UseGuards + @Roles + @ApiBearerAuth + @ApiResponse(401/403)` | Decorator composto `@Auth(Role.Dentist)` via `applyDecorators` |
| Extrair usuário do request | `@CurrentUser()` param decorator |
| `try/catch` + mapeamento de status em cada rota | `DomainExceptionFilter` único: `DomainError.code` → status HTTP |
| Montar `fetch`, timeout, headers, parse de erro por gateway | `DownstreamHttpClient` genérico (cada gateway só declara paths e mappers) |
| Nomes de headers e roles como strings soltas | `constants/headers.ts` e `Role` enum |
| Shape do erro repetido no Swagger | `ErrorResponseDto` único |
| Variáveis de ambiente lidas com `process.env` espalhado | `AppConfig` tipado (zod) injetado |
| Interface de use case reescrita | `UseCase<In, Out>` em `shared/application` |

> Regra de bom senso: só abstrair após a **segunda** ocorrência real. A lista acima já tem a segunda ocorrência garantida (futuros módulos de patient/employ).

---

## 5. Autenticação (Keycloak + JWKS)

### 5.1 Fluxo

```
Cliente ──Authorization: Bearer <JWT>──▶ BFF
                                          │
                           JwtAuthGuard (global)
                                          │ AuthenticateUseCase
                                          ▼
                              TokenVerifierPort (porta OUT)
                                          │
                       JoseJwksTokenVerifier (adapter)
                          │  jwtVerify(token, remoteJWKS, {issuer, algorithms:[RS256]})
                          │  JWKS: {KEYCLOAK_URL}/realms/{realm}/protocol/openid-connect/certs
                          ▼
                 AuthenticatedUser { id, username, email, roles[] }
                                          │
                    request.user = AuthenticatedUser
                                          │
                              RolesGuard (global) ──▶ Controller
```

### 5.2 Regras de validação do token

1. Header `Authorization: Bearer <token>` obrigatório → senão **401**.
2. Algoritmo permitido: **somente `RS256`** (rejeitar `none` e HS*).
3. `iss` deve ser igual a `KEYCLOAK_ISSUER`.
4. `exp`/`nbf` validados, com tolerância `JWT_CLOCK_TOLERANCE_SECONDS` (padrão 5s).
5. Audiência: se `KEYCLOAK_AUDIENCE` estiver definido, validar `aud`. Caso contrário, validar `azp === KEYCLOAK_CLIENT_ID`.
   *(O Keycloak, por padrão, emite `aud: "account"`; é preciso configurar um Audience Mapper para usar `aud`.)*
6. `kid` do header resolvido pelo conjunto de chaves remoto; `jose` faz cache e refetch automático em rotação de chave.
7. Qualquer falha de verificação → `InvalidTokenError` → **401** (sem vazar o motivo detalhado ao cliente; detalhe vai para o log em nível `warn`).

> **Atenção a ambientes Docker:** o `iss` do token é a URL pública do Keycloak, mas o BFF pode buscar o JWKS por uma URL interna. Por isso existem duas variáveis distintas: `KEYCLOAK_ISSUER` e `KEYCLOAK_JWKS_URI`.

### 5.3 Extração de roles

Função pura `extractRoles(claims, options): Role[]` em `auth/application/role-extractor.ts`:

- `KEYCLOAK_ROLES_SOURCE=realm` → lê `realm_access.roles`
- `KEYCLOAK_ROLES_SOURCE=client` → lê `resource_access[KEYCLOAK_CLIENT_ID].roles`
- Valores desconhecidos são **ignorados** (não quebram a requisição).
- Resultado deduplicado e restrito aos valores do enum `Role`.

### 5.4 Domínio de auth

```ts
// domain/role.enum.ts
export enum Role {
  Employ = 'employ',
  Dentist = 'dentist',
  Patient = 'patient',
}

// domain/authenticated-user.ts
export interface AuthenticatedUser {
  id: string;          // claim `sub` (UUID do Keycloak)
  username?: string;   // preferred_username
  email?: string;
  roles: Role[];
}
```

```ts
// application/ports/token-verifier.port.ts
export const TOKEN_VERIFIER = Symbol('TOKEN_VERIFIER');

export interface TokenVerifierPort {
  /** Lança InvalidTokenError se o token for inválido. */
  verify(token: string): Promise<AuthenticatedUser>;
}
```

---

## 6. Autorização (RBAC)

### 6.1 Matriz de acesso

| Recurso | `employ` | `dentist` | `patient` |
|---|:---:|:---:|:---:|
| `/api/dentist-services` (todas as operações) | ❌ | ✅ | ❌ |
| `/health` | público | público | público |
| `/docs` (Swagger) | conforme `SWAGGER_ENABLED` | | |

### 6.2 Comportamento

- Sem token / token inválido → **401** `{ "message": "..." }`
- Token válido sem a role exigida → **403** `{ "message": "..." }`
- Rota sem `@Public()` e sem `@Roles()`/`@Auth()` → **403** (fail-closed; evita esquecer de proteger uma rota nova).
- `@Roles(a, b)` significa **OU** (basta uma das roles).
- Uso no controller (nível de classe, aplicado a todas as rotas):

```ts
@Controller('api/dentist-services')
@Auth(Role.Dentist)
export class DentistServicesController { /* ... */ }
```

---

## 7. Contrato HTTP do BFF — Dentist Services

Base do downstream: `DENTIST_SERVICE_BASE_URL` (ex.: `http://localhost:5068`). Contrato de origem: `openapi.yml`.

Todas as rotas exigem `Authorization: Bearer <JWT>` com role **dentist**. O cliente **não** envia `X-Dentist-Id`; o BFF o injeta a partir do `sub` do token.

| Método | Rota BFF | Downstream | Header injetado | Sucesso | Erros |
|---|---|---|---|---|---|
| GET | `/api/dentist-services` | `GET /api/dentist-services` | `X-Dentist-Id` | 200 `DentistServiceResponse[]` | 401, 403, 502, 504 |
| GET | `/api/dentist-services/:id` | `GET /api/dentist-services/{id}` | `X-Dentist-Id` | 200 `DentistServiceResponse` | 400, 401, 403, 404 |
| POST | `/api/dentist-services` | `POST /api/dentist-services` | — (usa `dentistId` no body) | 201 `DentistServiceResponse` | 400, 401, 403 |
| PUT | `/api/dentist-services/:id` | `PUT /api/dentist-services/{id}` | ver §7.2 | 200 `DentistServiceResponse` | 400, 401, 403, 404 |
| DELETE | `/api/dentist-services/:id` | `DELETE /api/dentist-services/{id}` | `X-Dentist-Id` | 204 | 400, 401, 403, 404 |

### 7.1 Regras de negócio do BFF

1. **POST — anti-spoofing:** o body do cliente **não** contém `dentistId`. O BFF define `dentistId = user.id` antes de chamar o downstream. Se o cliente enviar `dentistId`, ele é descartado (`whitelist: true` + `forbidNonWhitelisted: false`, ou rejeitado com 400 — ver decisão D-3).
2. **`:id` deve ser UUID** (`ParseUUIDPipe`) → 400 caso contrário, sem chamar o downstream.
3. **Ownership:** a verificação final é do Dentist Service (403/404). O BFF apenas repassa a identidade correta e propaga o erro traduzido.

### 7.2 PUT e ownership (ponto de atenção)

No `openapi.yml`, o `PUT /api/dentist-services/{id}` **não declara** o header `X-Dentist-Id` nem resposta `403`, ao contrário de GET/DELETE. Isso significa que, sem tratamento, **qualquer dentista poderia atualizar o serviço de outro** se soubesse o id.

Estratégia do BFF (padrão, independente de mudança no downstream):

1. `UpdateDentistServiceUseCase` chama primeiro `gateway.getById(id, dentistId)`.
   - O downstream responde 403/404 se não for o dono ou não existir → o fluxo é interrompido.
2. Só então chama `gateway.update(id, dentistId, payload)`.

Recomendação paralela: corrigir o contrato downstream para exigir `X-Dentist-Id` no PUT e retornar 403. Quando isso acontecer, remover o passo 1 (basta alterar o use case; nenhum outro arquivo muda).

### 7.3 Schemas

**Enum `DentistServiceStatus`:** `disponivel` | `indisponivel` | `emBreve`

**`CreateDentistServiceRequest` (BFF, entrada do cliente)**

| Campo | Tipo | Regras |
|---|---|---|
| `name` | string | obrigatório, 3–100 caracteres |
| `price` | integer | obrigatório, ≥ 1 |
| `isPeriodic` | boolean | obrigatório |
| `status` | `DentistServiceStatus` | obrigatório |

**`UpdateDentistServiceRequest`:** mesmos campos acima (todos obrigatórios, conforme openapi — é `PUT`).

**`DentistServiceResponse`:**
`id` (uuid), `name`, `dentistId` (uuid), `price` (integer), `isPeriodic`, `status`, `createdAt` (ISO UTC), `updatedAt` (ISO UTC).

**`ErrorResponse`:** `{ "message": string }` — mesmo formato do downstream e de todos os erros do BFF.

### 7.4 Mappers (anti-corruption)

- `downstream-dentist-service.mapper.ts`: JSON do downstream ⇄ entidade `DentistService` (único lugar que conhece o shape do downstream; datas convertidas para `Date`).
- `dentist-service.http-mapper.ts`: entidade ⇄ DTO de resposta do BFF.

Mesmo hoje sendo quase 1:1, a separação isola o BFF de mudanças no contrato downstream e permite agregação futura (ex.: enriquecer com dados de outros serviços).

---

## 8. Portas e casos de uso

### 8.1 Porta OUT — gateway do Dentist Service

```ts
export const DENTIST_SERVICES_GATEWAY = Symbol('DENTIST_SERVICES_GATEWAY');

export interface DentistServicesGateway {
  list(dentistId: string): Promise<DentistService[]>;
  getById(id: string, dentistId: string): Promise<DentistService>;
  create(input: CreateDentistServiceInput): Promise<DentistService>;
  update(id: string, dentistId: string, input: UpdateDentistServiceInput): Promise<DentistService>;
  delete(id: string, dentistId: string): Promise<void>;
}
```

### 8.2 Casos de uso (portas IN)

```ts
// shared/application/use-case.ts
export interface UseCase<In, Out> {
  execute(input: In): Promise<Out>;
}
```

| Use case | Input | Output |
|---|---|---|
| `ListDentistServicesUseCase` | `{ dentistId }` | `DentistService[]` |
| `GetDentistServiceByIdUseCase` | `{ id, dentistId }` | `DentistService` |
| `CreateDentistServiceUseCase` | `{ dentistId, name, price, isPeriodic, status }` | `DentistService` |
| `UpdateDentistServiceUseCase` | `{ id, dentistId, name, price, isPeriodic, status }` | `DentistService` |
| `DeleteDentistServiceUseCase` | `{ id, dentistId }` | `void` |

Os use cases **não conhecem** HTTP, JWT nem `fetch`; recebem `dentistId` já resolvido pelo controller a partir de `@CurrentUser()`.

---

## 9. Tratamento de erros

### 9.1 Hierarquia

```ts
// shared/domain/domain-error.ts
export abstract class DomainError extends Error {
  abstract readonly code: string;
}
```

| Classe | `code` | HTTP |
|---|---|---|
| `InvalidTokenError` | `INVALID_TOKEN` | 401 |
| `InsufficientRoleError` | `INSUFFICIENT_ROLE` | 403 |
| `DentistServiceForbiddenError` | `RESOURCE_FORBIDDEN` | 403 |
| `DentistServiceNotFoundError` | `RESOURCE_NOT_FOUND` | 404 |
| `DownstreamValidationError` | `DOWNSTREAM_BAD_REQUEST` | 400 |
| `DownstreamUnavailableError` | `DOWNSTREAM_UNAVAILABLE` | 502 |
| `DownstreamTimeoutError` | `DOWNSTREAM_TIMEOUT` | 504 |

### 9.2 Tradução no `DownstreamHttpClient` / gateway

| Resposta do downstream | Erro lançado |
|---|---|
| 400 | `DownstreamValidationError` (propaga `message` do downstream) |
| 403 | `DentistServiceForbiddenError` |
| 404 | `DentistServiceNotFoundError` |
| 5xx / falha de rede | `DownstreamUnavailableError` |
| timeout (`AbortError`) | `DownstreamTimeoutError` |
| outro 4xx inesperado | `DownstreamUnavailableError` (logado com status real) |

O gateway específico só decide **qual** erro de recurso usar para 403/404; o cliente genérico cuida de rede, timeout, JSON inválido e 5xx.

### 9.3 Filtro global

`DomainExceptionFilter` converte `DomainError` → `{ message }` com o status da tabela 9.1. `HttpException` do Nest (ex.: falha de `ValidationPipe`, `ParseUUIDPipe`) → mesmo formato `{ message }` (se `message` for array, juntar em string). Erros não mapeados → 500 `{ message: "Erro interno." }` com stack apenas no log.

---

## 10. Configuração (variáveis de ambiente)

Validadas com zod no boot; qualquer inconsistência impede a aplicação de subir.

| Variável | Obrigatória | Exemplo | Descrição |
|---|:---:|---|---|
| `PORT` | não | `3000` | Porta HTTP do BFF |
| `NODE_ENV` | não | `production` | Ambiente |
| `KEYCLOAK_ISSUER` | ✅ | `https://auth.exemplo.com/realms/clinica` | Valor esperado em `iss` |
| `KEYCLOAK_JWKS_URI` | ✅ | `https://auth.exemplo.com/realms/clinica/protocol/openid-connect/certs` | Endpoint JWKS |
| `KEYCLOAK_CLIENT_ID` | ✅ | `bff-clinica` | Usado em `azp` e para roles de client |
| `KEYCLOAK_AUDIENCE` | não | `bff-clinica` | Se definido, valida `aud` |
| `KEYCLOAK_ROLES_SOURCE` | não | `realm` (padrão) \| `client` | Onde ler as roles |
| `JWT_CLOCK_TOLERANCE_SECONDS` | não | `5` | Tolerância de relógio |
| `JWKS_CACHE_MAX_AGE_MS` | não | `600000` | Cache do JWKS |
| `JWKS_COOLDOWN_MS` | não | `30000` | Intervalo mínimo entre refetches por `kid` desconhecido |
| `DENTIST_SERVICE_BASE_URL` | ✅ | `http://localhost:5068` | Base do Dentist Service |
| `DOWNSTREAM_TIMEOUT_MS` | não | `5000` | Timeout padrão de chamadas downstream |
| `CORS_ORIGINS` | não | `https://app.exemplo.com` | Lista separada por vírgula |
| `SWAGGER_ENABLED` | não | `true` | Habilita `/docs` |
| `THROTTLE_TTL_SECONDS` / `THROTTLE_LIMIT` | não | `60` / `100` | Rate limit |

Entregar `.env.example` com todas as variáveis.

---

## 11. Requisitos não funcionais

- **Segurança:** `helmet`; CORS restrito por `CORS_ORIGINS`; rate limit global; nunca logar o token nem o header `Authorization`; `ValidationPipe` global com `whitelist: true`, `transform: true`.
- **Observabilidade:** `X-Correlation-Id` aceito ou gerado, propagado ao downstream e incluído em todos os logs; log de cada chamada downstream (método, path, status, duração — sem payload sensível).
- **Resiliência:** timeout em toda chamada downstream; **sem retry** em métodos não idempotentes (POST). Retry opcional (1x) para GET apenas em falha de rede.
- **Health:** `GET /health` (público) com liveness; readiness opcional checando o JWKS e o downstream.
- **Stateless:** nenhuma sessão ou cache de usuário em memória além do cache de JWKS do `jose`.

---

## 12. Estratégia de testes

### 12.1 Níveis, escopo e ferramentas

| Nível | Escopo | Dependências reais | Ferramenta | Pasta |
|---|---|---|---|---|
| **Unitário — domain** | enums, erros, regras puras | nenhuma | Jest | `src/**/domain/**/*.spec.ts` |
| **Unitário — application** | use cases e `extractRoles` | nenhuma (portas substituídas por **fakes**) | Jest | `src/**/application/**/*.spec.ts` |
| **Unitário — adapters** | gateways, mappers, `DownstreamHttpClient`, guards, filtro, decorators | nenhuma (`fetch` mockado) | Jest | `src/**/infrastructure/**/*.spec.ts` |
| **Integração** | `JoseJwksTokenVerifier` + JWKS; `DownstreamHttpClient` + servidor HTTP local | servidores locais efêmeros | Jest | `test/integration/` |
| **E2E** | aplicação Nest completa via HTTP (RBAC + rotas + erros) | JWKS local + downstream mock local | Jest + Supertest | `test/e2e/` |
| **Arquitetura** | regras de dependência entre camadas | — | dependency-cruiser | `.dependency-cruiser.js` |

O Jest deve ser configurado com **3 projects** (`unit`, `integration`, `e2e`) para rodar separadamente (`npm run test:unit`, `test:int`, `test:e2e`) e juntos (`npm test`).

### 12.2 Convenções

- Padrão **Arrange / Act / Assert**, um comportamento por `it`.
- Nomes em português descrevendo comportamento: `it('não chama update quando o serviço pertence a outro dentista')`.
- **Fakes em memória** para portas (preferidos a mocks do Jest): verificam estado e chamadas, sobrevivem a refactors.
- `jest.fn()` apenas para fronteiras que não são portas (ex.: `fetch` global).
- **Builders** para dados de teste (`aDentistService()`, `anAuthenticatedUser()`), evitando objetos literais repetidos.
- Sem `sleep`: tempo controlado com `jest.useFakeTimers()` ou servidor de teste que atrasa a resposta.
- Testes independentes e sem ordem implícita; nenhuma porta fixa (servidores de teste usam porta `0`).
- Nenhum teste lê `process.env` real: a config é construída por `buildTestConfig(overrides)`.

### 12.3 Test kit compartilhado (DRY nos testes)

Tudo em `test/support/`, importado por unit, integração e E2E:

| Arquivo | Responsabilidade |
|---|---|
| `fakes/fake-dentist-services.gateway.ts` | Implementa `DentistServicesGateway` em memória; registra chamadas; permite forçar erro por método |
| `fakes/fake-token-verifier.ts` | Implementa `TokenVerifierPort`; devolve usuário configurado ou lança `InvalidTokenError` |
| `builders/dentist-service.builder.ts` | `aDentistService({ ...overrides })` |
| `builders/authenticated-user.builder.ts` | `anAuthenticatedUser({ roles: [Role.Dentist] })` |
| `jwt-test-kit.ts` | Gera par RSA (`jose.generateKeyPair`), sobe servidor JWKS local, `signToken({ sub, roles, iss, azp, aud, exp, nbf, alg, kid })` |
| `downstream-mock.ts` | Servidor HTTP que simula o Dentist Service conforme `openapi.yml`; registra requisições; permite forçar 400/403/404/5xx/atraso |
| `test-config.ts` | `buildTestConfig(overrides)` com valores válidos do §10 |

Exemplo de fake (esqueleto):

```ts
export class FakeDentistServicesGateway implements DentistServicesGateway {
  readonly items = new Map<string, DentistService>();
  readonly calls = { list: [], getById: [], create: [], update: [], delete: [] } as Record<string, unknown[][]>;
  private failures = new Map<string, Error>();

  failNext(method: keyof DentistServicesGateway, error: Error) { this.failures.set(method, error); }

  private record(method: string, args: unknown[]) {
    this.calls[method].push(args);
    const err = this.failures.get(method);
    if (err) { this.failures.delete(method); throw err; }
  }

  async getById(id: string, dentistId: string) {
    this.record('getById', [id, dentistId]);
    const item = this.items.get(id);
    if (!item) throw new DentistServiceNotFoundError(id);
    if (item.dentistId !== dentistId) throw new DentistServiceForbiddenError();
    return item;
  }
  // list, create, update, delete seguem o mesmo molde
}
```

### 12.4 Testes da camada `application` (núcleo da suíte)

Sem Nest, sem HTTP, sem rede. Cada use case é instanciado com `new` recebendo o fake do gateway.

**`extractRoles`**

| # | Cenário | Esperado |
|---|---|---|
| 1 | `source=realm`, `realm_access.roles=['dentist']` | `[Role.Dentist]` |
| 2 | `source=client`, `resource_access[clientId].roles=['patient']` | `[Role.Patient]` |
| 3 | `source=realm` mas roles só em `resource_access` | `[]` |
| 4 | Claim `realm_access` ausente | `[]` |
| 5 | Roles desconhecidas (`offline_access`, `uma_authorization`, `admin`) | ignoradas |
| 6 | Roles duplicadas | deduplicadas |
| 7 | `roles` não é array (string/objeto/null) | `[]`, sem lançar |
| 8 | Várias roles válidas | todas, em ordem estável |
| 9 | Caixa diferente (`Dentist`) | **não** reconhece (case-sensitive) |

**`AuthenticateUseCase`**

| # | Cenário | Esperado |
|---|---|---|
| 1 | Header `Bearer <token>` válido | chama `verify(token)` e devolve o usuário |
| 2 | Header `undefined`/vazio | `InvalidTokenError`; `verify` **não** chamado |
| 3 | Esquema errado (`Basic xxx`) | `InvalidTokenError` |
| 4 | `Bearer` sem token / com espaços extras | `InvalidTokenError` |
| 5 | `bearer` em minúsculas | aceito (esquema case-insensitive, conforme RFC 6750) |
| 6 | `verify` lança `InvalidTokenError` | propaga o mesmo erro |

**`ListDentistServicesUseCase`**

| # | Cenário | Esperado |
|---|---|---|
| 1 | Dentista com 2 serviços | retorna 2; gateway chamado com o `dentistId` |
| 2 | Dentista sem serviços | `[]` |
| 3 | Gateway lança `DownstreamUnavailableError` | propaga sem alterar |

**`GetDentistServiceByIdUseCase`**

| # | Cenário | Esperado |
|---|---|---|
| 1 | Serviço do próprio dentista | retorna o serviço |
| 2 | Serviço inexistente | `DentistServiceNotFoundError` |
| 3 | Serviço de outro dentista | `DentistServiceForbiddenError` |

**`CreateDentistServiceUseCase`**

| # | Cenário | Esperado |
|---|---|---|
| 1 | Dados válidos | gateway recebe `dentistId` do input + campos; retorna o criado |
| 2 | Gateway lança `DownstreamValidationError` | propaga com a mensagem original |
| 3 | Todos os valores do enum `status` | aceitos e repassados sem alteração |

**`UpdateDentistServiceUseCase`** (regra crítica do §7.2)

| # | Cenário | Esperado |
|---|---|---|
| 1 | Serviço do próprio dentista | chama `getById` **antes** de `update` (verificar ordem); retorna atualizado |
| 2 | Serviço de outro dentista | `DentistServiceForbiddenError`; **`update` nunca chamado** |
| 3 | Serviço inexistente | `DentistServiceNotFoundError`; **`update` nunca chamado** |
| 4 | `getById` ok e `update` lança `DownstreamValidationError` | propaga |
| 5 | `getById` ok e `update` lança `DownstreamTimeoutError` | propaga |
| 6 | `update` recebe `id`, `dentistId` e payload corretos | verificado via `calls.update` |

**`DeleteDentistServiceUseCase`**

| # | Cenário | Esperado |
|---|---|---|
| 1 | Serviço do próprio dentista | gateway `delete` chamado; resolve `void` |
| 2 | Inexistente | `DentistServiceNotFoundError` |
| 3 | De outro dentista | `DentistServiceForbiddenError`; serviço permanece no fake |

Exemplo:

```ts
describe('UpdateDentistServiceUseCase', () => {
  it('não chama update quando o serviço pertence a outro dentista', async () => {
    const gateway = new FakeDentistServicesGateway();
    const service = aDentistService({ dentistId: 'dentist-B' });
    gateway.items.set(service.id, service);
    const sut = new UpdateDentistServiceUseCase(gateway);

    await expect(
      sut.execute({ id: service.id, dentistId: 'dentist-A', name: 'Novo', price: 200, isPeriodic: false, status: DentistServiceStatus.Indisponivel }),
    ).rejects.toBeInstanceOf(DentistServiceForbiddenError);

    expect(gateway.calls.update).toHaveLength(0);
  });
});
```

### 12.5 Testes de domain

- Cada `DomainError` expõe o `code` esperado (tabela §9.1) e é `instanceof DomainError` e `Error`.
- Enums `Role` e `DentistServiceStatus` contêm **exatamente** os valores do §5.4 e do `openapi.yml` (teste de snapshot de valores, protege contra renomeação acidental).

### 12.6 Testes de adapters (unitários)

**`DownstreamHttpClient`** (com `fetch` mockado)

| # | Cenário | Esperado |
|---|---|---|
| 1 | 200 com JSON | corpo parseado |
| 2 | 201 com JSON | corpo parseado |
| 3 | 204 sem corpo | `undefined`/`void`, sem erro de parse |
| 4 | 400 `{ message }` | `DownstreamValidationError` com a mensagem |
| 5 | 403 / 404 | erro com `status` para o gateway traduzir |
| 6 | 500 / 503 | `DownstreamUnavailableError` |
| 7 | Falha de rede (`TypeError`) | `DownstreamUnavailableError` |
| 8 | Timeout (`AbortError`/`TimeoutError`) | `DownstreamTimeoutError` |
| 9 | Resposta 2xx com JSON inválido | `DownstreamUnavailableError`, sem vazar o corpo |
| 10 | Header `X-Correlation-Id` do contexto | enviado ao downstream |
| 11 | Headers e body | `Content-Type: application/json` apenas quando há body |

**`HttpDentistServicesGateway`** — para **cada** operação verificar método, URL, headers e body:

| Operação | Request esperada |
|---|---|
| `list` | `GET /api/dentist-services` + `X-Dentist-Id` |
| `getById` | `GET /api/dentist-services/{id}` + `X-Dentist-Id` |
| `create` | `POST /api/dentist-services` + body com `dentistId` |
| `update` | `PUT /api/dentist-services/{id}` + body sem `dentistId` |
| `delete` | `DELETE /api/dentist-services/{id}` + `X-Dentist-Id` |

Mais: 403 → `DentistServiceForbiddenError`; 404 → `DentistServiceNotFoundError`; `id` com caracteres especiais é codificado na URL.

**Mappers**
- Downstream → entidade: converte `createdAt`/`updatedAt` para `Date`; preserva todos os campos; ignora campos extras desconhecidos.
- Entidade → DTO de resposta: datas em ISO UTC; shape idêntico ao `DentistServicesResponse`.

**Guards, decorators e filtro**
- `JwtAuthGuard`: rota `@Public()` libera sem token; sem token → 401; token válido grava `request.user`.
- `RolesGuard`: sem `@Roles` em rota não pública → nega (fail-closed); role correta → permite; role errada → 403; `@Roles(a, b)` com usuário só com `b` → permite (OU); metadata de método sobrepõe a de classe.
- `@Auth(Role.Dentist)`: aplica `Roles` e metadados do Swagger (verificar via `Reflector`).
- `DomainExceptionFilter`: cada `code` do §9.1 → status correto e corpo `{ message }`; `HttpException` com `message` array → string única; erro desconhecido → 500 com mensagem genérica e **sem** stack no corpo.
- DTOs (`ValidationPipe`): `name` com 2 e 101 caracteres → inválido; 3 e 100 → válido; `price` 0, negativo, decimal, string → inválido; `isPeriodic` não booleano → inválido; `status` fora do enum → inválido; campo `dentistId` enviado → descartado.

### 12.7 Testes de integração

**`JoseJwksTokenVerifier` + JWKS local (`jwt-test-kit`)**

| # | Token | Esperado |
|---|---|---|
| 1 | Válido (RS256, `iss` e `azp`/`aud` corretos) | `AuthenticatedUser` com `id = sub` e roles corretas |
| 2 | Expirado | `InvalidTokenError` |
| 3 | `nbf` no futuro | `InvalidTokenError` |
| 4 | Dentro da tolerância de relógio | aceito |
| 5 | Assinado por chave não publicada no JWKS | `InvalidTokenError` |
| 6 | `iss` diferente | `InvalidTokenError` |
| 7 | `azp`/`aud` diferente | `InvalidTokenError` |
| 8 | `alg=none` | `InvalidTokenError` |
| 9 | `alg=HS256` assinado com a chave pública como segredo (ataque de confusão de algoritmo) | `InvalidTokenError` |
| 10 | `kid` desconhecido, depois chave publicada no JWKS (rotação) | refetch e aceita, respeitando o cooldown |
| 11 | JWKS fora do ar | `InvalidTokenError` (não 500) |
| 12 | Token malformado / string aleatória | `InvalidTokenError` |
| 13 | Nenhuma exceção ou log contém o token | verificado em spy de logger |

**`DownstreamHttpClient` + servidor local:** timeout real (servidor atrasa > `DOWNSTREAM_TIMEOUT_MS`), conexão recusada, resposta truncada.

### 12.8 Testes E2E (Supertest + app Nest completa)

JWKS local e `downstream-mock` iniciados em `beforeAll`; `AppModule` real com `buildTestConfig` apontando para eles.

**Autenticação e RBAC**

| # | Cenário | Esperado |
|---|---|---|
| 1 | Qualquer rota de `/api/dentist-services` sem `Authorization` | 401 `{ message }` |
| 2 | Token expirado / assinatura inválida / `iss` errado / `alg=none` | 401 |
| 3 | Role `patient` | 403 |
| 4 | Role `employ` | 403 |
| 5 | Role `dentist` | 2xx |
| 6 | Token válido sem role alguma / só roles desconhecidas | 403 |
| 7 | Controller de teste sem `@Auth`/`@Public` (fail-closed) | 403 |
| 8 | `GET /health` sem token | 200 |

**Rotas (com role `dentist`)**

| # | Cenário | Esperado |
|---|---|---|
| 9 | `GET /api/dentist-services` | 200; downstream recebeu `X-Dentist-Id = sub` |
| 10 | `GET /:id` do próprio dentista | 200 com o shape do `DentistServiceResponse` |
| 11 | `GET /:id` de outro dentista | 403 `{ message }` |
| 12 | `GET /:id` inexistente | 404 `{ message }` |
| 13 | `POST` válido | 201; downstream recebeu `dentistId = sub` |
| 14 | `POST` com `dentistId` de outro dentista no body | `dentistId` enviado ao downstream continua sendo o `sub` |
| 15 | `POST` com `name` curto / `price` 0 / `status` inválido | 400 e downstream **não** recebe a chamada |
| 16 | `PUT` válido no próprio serviço | 200; downstream recebeu `GET` e depois `PUT` (nessa ordem) |
| 17 | `PUT` em serviço de outro dentista | 403 e downstream **não** recebeu `PUT` |
| 18 | `PUT` em inexistente | 404 e downstream **não** recebeu `PUT` |
| 19 | `DELETE` válido | 204 sem corpo |
| 20 | `DELETE` de outro dentista / inexistente | 403 / 404 |
| 21 | `:id` não-UUID em GET/PUT/DELETE | 400 sem chamar o downstream |

**Falhas do downstream**

| # | Cenário | Esperado |
|---|---|---|
| 22 | Downstream 400 `{ message }` | 400 com a mesma mensagem |
| 23 | Downstream 500 | 502 `{ message }` |
| 24 | Downstream fora do ar (conexão recusada) | 502 |
| 25 | Downstream lento (> timeout) | 504 |
| 26 | Todos os erros acima | corpo exatamente `{ "message": string }` |

**Transversais**
- `X-Correlation-Id` enviado pelo cliente é devolvido na resposta e repassado ao downstream; se ausente, é gerado.
- Header `Authorization` nunca aparece nos logs capturados.
- Excesso de requisições → 429 no formato `{ message }`.
- Headers do `helmet` presentes nas respostas.

### 12.9 Testes de arquitetura

Executados por `npm run arch:check`:

- `domain` não importa `application`, `infrastructure`, `@nestjs/*`, `jose`.
- `application` não importa `infrastructure` nem pacotes HTTP; só `@nestjs/common` para `@Injectable`/`@Inject`.
- Um módulo não importa a `infrastructure` de outro módulo (comunicação apenas por portas ou `shared`).
- Nenhum ciclo de dependência.

### 12.10 Metas de cobertura e portões de CI

Configurar `coverageThreshold` no Jest:

| Escopo | Linhas | Branches |
|---|---|---|
| `src/**/domain/**` | ≥ 95% | ≥ 90% |
| `src/**/application/**` | ≥ 90% | ≥ 85% |
| Global | ≥ 80% | ≥ 75% |

O pipeline falha se: algum teste falhar, cobertura ficar abaixo das metas, `arch:check` falhar, ou houver teste marcado com `.only`/`.skip` (regra de lint `jest/no-focused-tests` e `jest/no-disabled-tests`).

Cobertura é um piso, não o objetivo: os cenários das tabelas acima são o critério de aceitação; um use case só é considerado testado se **todas** as suas linhas de tabela existirem como `it`.

---

## 13. Decisões em aberto e riscos

| ID | Tema | Situação / Recomendação |
|---|---|---|
| D-1 | Grafia da role `employ` | Mantida como informado. Confirmar se no Keycloak é `employ` ou `employee`; o valor do enum precisa ser **idêntico** (case-sensitive) ao nome da role. |
| D-2 | `dentistId` = `sub` do Keycloak? | Assumido que o GUID do dentista no Dentist Service é o `sub` do usuário. Se for outro identificador, usar um claim customizado (`dentist_id`) via mapper no Keycloak e trocar a leitura no `AuthenticatedUser`. |
| D-3 | `dentistId` enviado pelo cliente no POST | Padrão: descartar silenciosamente (`whitelist`). Alternativa mais estrita: rejeitar com 400. |
| D-4 | PUT sem ownership no downstream | Mitigado no BFF (§7.2). Corrigir o contrato downstream é recomendado. |
| D-5 | Semântica de `price` | O `openapi.yml` diz "centavos ou valor inteiro" (ambíguo). O BFF repassa o inteiro sem converter. Definir com o time do Dentist Service antes de expor ao frontend. |
| D-6 | Roles em realm vs client | Padrão `realm`. Alterável por `KEYCLOAK_ROLES_SOURCE` sem mudar código. |
| D-7 | Endpoint `GET` lista usa `X-Dentist-Id` | O BFF nunca expõe filtro por outro dentista. Se `employ` precisar listar serviços de qualquer dentista no futuro, criar rota/caso de uso **separado**, não relaxar este. |

---

## 14. Extensibilidade (como adicionar um novo módulo)

Para um futuro `patient-services` ou `employ-*`:

1. Criar `modules/<nome>/{domain,application,infrastructure}` seguindo o mesmo molde.
2. Declarar a porta OUT e os use cases.
3. Implementar o gateway reutilizando `DownstreamHttpClient`.
4. Anotar o controller com `@Auth(Role.X)`.
5. Nada muda em `auth/` ou em `shared/` (exceto, se necessário, novos `DomainError`s mapeados no filtro).