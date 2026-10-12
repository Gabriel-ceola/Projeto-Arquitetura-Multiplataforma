# TASKS — BFF Odontológico (NestJS · Hexagonal · Keycloak RBAC)

> Referência: `spec.md` (seções citadas como §N).
> Convenção: `[ ]` pendente · `[x]` concluída. Cada tarefa tem **DoD** (Definition of Done).
> Execute as fases na ordem; tarefas dentro de uma fase marcadas com ⚡ podem ser feitas em paralelo.

---

## Mapa de dependências

```
F0 Bootstrap
 └─ F1 Shared Kernel ─┬─ F2 Auth ──────────┐
                      └─ F3 Dentist domain ├─ F5 Adapter HTTP IN ─ F6 Cross-cutting ─ F7 Testes ─ F8 Entrega
                         F4 Gateway OUT ───┘

T-07.1/T-07.2 (config Jest + test kit) devem ser feitas logo após a F1,
para que F2–F6 já entreguem código + testes juntos.
```

---

## F0 — Bootstrap do projeto

- [x] **T-00.1** Configurar projeto , `strict: true` no `tsconfig`, path alias `@shared/*` e `@modules/*`.
      **DoD:** `npm run build` e `npm run start:dev` funcionam; aliases resolvem no build e no Jest.
- [x] **T-00.2** Instalar dependências: `@nestjs/config zod jose class-validator class-transformer @nestjs/swagger helmet @nestjs/throttler nestjs-pino pino-http @nestjs/terminus`; dev: `supertest dependency-cruiser`.
      **DoD:** `package.json` com versões fixadas; sem warnings de peer deps.
- [x] **T-00.3** Configurar ESLint + Prettier + `husky`/`lint-staged` (opcional).
      **DoD:** `npm run lint` limpo.
- [x] **T-00.4** Criar a árvore de pastas do §3.2 (com `.gitkeep` onde vazio).
      **DoD:** estrutura idêntica ao spec.
- [x] **T-00.5** Criar `.env.example` com todas as variáveis do §10 e `.gitignore` incluindo `.env`.
      **DoD:** nenhuma variável do §10 ausente.

---

## F1 — Shared Kernel (base do DRY)

- [x] **T-01.1** ⚡ `shared/domain/domain-error.ts`: classe abstrata `DomainError` com `code`.
      `shared/domain/common-errors.ts`: `DownstreamValidationError`, `DownstreamUnavailableError`, `DownstreamTimeoutError` (§9.1).
      **DoD:** erros sem dependência de Nest; teste unitário mínimo de `code`/`message`.
- [x] **T-01.2** ⚡ `shared/application/use-case.ts`: interface `UseCase<In, Out>`.
      **DoD:** exportada e usada pelos use cases das fases seguintes.
- [x] **T-01.3** `shared/config/env.schema.ts` (zod) + `app-config.module.ts` expondo um `AppConfig` tipado injetável (token `APP_CONFIG`).
      **DoD:** subir com env inválida/ausente **falha no boot** com mensagem listando as variáveis; defaults do §10 aplicados; teste unitário do schema.
- [x] **T-01.4** ⚡ `shared/infrastructure/constants/headers.ts`: `X_DENTIST_ID`, `X_CORRELATION_ID`, `AUTHORIZATION`.
      **DoD:** nenhuma string literal de header fora deste arquivo (verificar com grep).
- [x] **T-01.5** Configurar `dependency-cruiser` com as regras do §3.1 (domain ← application ← infrastructure; `shared` e `modules/*` não importam `infrastructure` de outro módulo) e script `npm run arch:check`.
      **DoD:** script falha ao introduzir import proibido (provar com um import de teste e reverter); incluído no CI (F8).
- [x] **T-01.6** `shared/infrastructure/http/error-response.dto.ts` (`ErrorResponseDto { message }` com Swagger).
      **DoD:** reutilizável por decorators Swagger.

---

## F2 — Módulo Auth (Keycloak + JWKS + RBAC)

### Domínio e aplicação

- [x] **T-02.1** ⚡ `auth/domain/role.enum.ts` (`employ`, `dentist`, `pacient`) e `authenticated-user.ts` (§5.4).
      **DoD:** valores do enum confirmados com o Keycloak real (decisão D-1).
- [x] **T-02.2** ⚡ `auth/domain/auth-errors.ts`: `InvalidTokenError` (`INVALID_TOKEN`), `InsufficientRoleError` (`INSUFFICIENT_ROLE`).
      **DoD:** estendem `DomainError`.
- [x] **T-02.3** `auth/application/ports/token-verifier.port.ts` (`TOKEN_VERIFIER` + interface).
      **DoD:** sem import de `jose`.
- [x] **T-02.4** `auth/application/role-extractor.ts`: função pura `extractRoles(claims, { source, clientId })` (§5.3).
      **DoD:** testes cobrindo: realm; client; claim ausente; roles desconhecidas ignoradas; duplicadas deduplicadas; tipos inesperados (string em vez de array) → `[]`.
- [x] **T-02.5** `auth/application/authenticate.use-case.ts`: recebe o valor do header `Authorization`, valida o formato `Bearer <token>`, chama `TokenVerifierPort`.
      **DoD:** header ausente/mal formado → `InvalidTokenError`; testes com fake do port.

### Infraestrutura

- [x] **T-02.6** `auth/infrastructure/jose-jwks-token-verifier.adapter.ts`:
  - `createRemoteJWKSet(new URL(KEYCLOAK_JWKS_URI), { cacheMaxAge, cooldownDuration, timeoutDuration })` criado **uma vez** (singleton do módulo).
  - `jwtVerify` com `issuer`, `algorithms: ['RS256']`, `clockTolerance`, e `audience` **ou** checagem de `azp` (§5.2).
  - Mapeia claims → `AuthenticatedUser` usando `extractRoles`.
  - Qualquer exceção do `jose` → `InvalidTokenError` (motivo real apenas em log `warn`, sem o token).
    **DoD:** testes de integração (T-07.19) passam; nenhum log contém o token.
- [x] **T-02.7** ⚡ Decorators em `auth/infrastructure/decorators/`:
      `@Public()`, `@Roles(...roles)`, `@CurrentUser()`, e o composto `@Auth(...roles)` (= `Roles` + `ApiBearerAuth` + `ApiUnauthorizedResponse` + `ApiForbiddenResponse` com `ErrorResponseDto`).
      **DoD:** `@Auth(Role.Dentist)` sozinho no controller basta para proteger e documentar.
- [x] **T-02.8** `jwt-auth.guard.ts`: se a rota for `@Public()` libera; senão executa `AuthenticateUseCase` e grava `request.user`.
      **DoD:** 401 em header ausente/inválido; rotas públicas liberadas.
- [x] **T-02.9** `roles.guard.ts`: lê `@Roles` (handler → classe); sem metadata e rota não pública → **nega com `InsufficientRoleError`** (fail-closed, §6.2); com metadata → exige interseção de roles.
      **DoD:** teste unitário para: sem metadata, role correta, role errada, múltiplas roles (OU).
- [x] **T-02.10** `auth.module.ts`: provê `TOKEN_VERIFIER → JoseJwksTokenVerifier`, `AuthenticateUseCase`, e registra `JwtAuthGuard` e `RolesGuard` como `APP_GUARD` (nesta ordem).
      **DoD:** guards globais ativos sem precisar de `@UseGuards` em nenhum controller.

---

## F3 — Dentist Services: domínio e aplicação

- [x] **T-03.1** ⚡ `domain/dentist-service-status.enum.ts` (`disponivel`, `indisponivel`, `emBreve`) e `domain/dentist-service.ts` (campos do §7.3, datas como `Date`).
      **DoD:** sem dependências externas.
- [x] **T-03.2** ⚡ `domain/dentist-service-errors.ts`: `DentistServiceNotFoundError` (`RESOURCE_NOT_FOUND`), `DentistServiceForbiddenError` (`RESOURCE_FORBIDDEN`).
      **DoD:** estendem `DomainError`.
- [x] **T-03.3** `application/ports/dentist-services.gateway.ts` (§8.1) + tipos `CreateDentistServiceInput` / `UpdateDentistServiceInput`.
      **DoD:** apenas tipos do domínio; sem `fetch`.
- [x] **T-03.4** Use cases (§8.2), cada um com `@Injectable()` e `@Inject(DENTIST_SERVICES_GATEWAY)`:
  - [x] `ListDentistServicesUseCase`
  - [x] `GetDentistServiceByIdUseCase`
  - [x] `CreateDentistServiceUseCase`
  - [x] `UpdateDentistServiceUseCase` — **faz `getById(id, dentistId)` antes do `update`** (§7.2)
  - [x] `DeleteDentistServiceUseCase`

  **DoD:** testes unitários com fake do gateway; no Update, provar que `update` **não** é chamado quando `getById` lança 403/404.

---

## F4 — Adapter OUT: cliente HTTP downstream

- [x] **T-04.1** `shared/infrastructure/http/downstream-http-client.ts` (genérico e reutilizável):
  - construtor recebe `baseUrl`, `timeoutMs`, headers padrão;
  - método `request<T>({ method, path, headers?, body?, expectedStatus? })`;
  - injeta `X-Correlation-Id` do contexto da requisição;
  - `AbortSignal.timeout` → `DownstreamTimeoutError`;
  - falha de rede/5xx → `DownstreamUnavailableError`;
  - 400 → `DownstreamValidationError` com `message` do corpo `{ message }`;
  - 403/404 e outros status são **devolvidos como resultado tipado** (ou lançados com um erro interno `DownstreamHttpError { status, message }`) para o gateway específico traduzir;
  - parse tolerante: corpo vazio (204) e JSON inválido não quebram.

  **DoD:** testes unitários com `fetch` mockado cobrindo todos os ramos do §9.2.

- [x] **T-04.2** `shared/infrastructure/http/downstream-http.module.ts`: factory que permite criar clientes por serviço (`DownstreamHttpClient` configurado com a base URL de cada downstream).
      **DoD:** adicionar um novo downstream = uma linha de configuração.
- [x] **T-04.3** `dentist-services/infrastructure/gateways/downstream-dentist-service.mapper.ts`: JSON downstream ⇄ entidade.
      **DoD:** converte `createdAt/updatedAt` para `Date`; teste unitário.
- [x] **T-04.4** `http-dentist-services.gateway.ts` implementando `DentistServicesGateway`:
  - `list` → `GET /api/dentist-services` + `X-Dentist-Id`
  - `getById` → `GET /api/dentist-services/{id}` + `X-Dentist-Id`
  - `create` → `POST /api/dentist-services` (body com `dentistId`)
  - `update` → `PUT /api/dentist-services/{id}` (+ `X-Dentist-Id` — inofensivo e pronto para quando o contrato for corrigido)
  - `delete` → `DELETE /api/dentist-services/{id}` + `X-Dentist-Id`
  - 403 → `DentistServiceForbiddenError`, 404 → `DentistServiceNotFoundError` (usando `message` do downstream quando existir).

  **DoD:** testes verificam método, path, headers e body de **cada** operação e a tradução de 403/404.

---

## F5 — Adapter IN: HTTP do Dentist Services

- [x] **T-05.1** ⚡ DTOs de entrada (`class-validator` + Swagger):
  - `CreateDentistServiceDto`: `name` (string 3–100), `price` (int ≥ 1), `isPeriodic` (boolean), `status` (`IsEnum`);
  - `UpdateDentistServiceDto`: mesmos campos.

  Reaproveitar um único conjunto de regras (ex.: classe base `DentistServiceWritableDto` e `UpdateDentistServiceDto extends` ela) para não duplicar validações.
  **DoD:** `dentistId` **não** existe no DTO de criação (§7.1).

- [x] **T-05.2** ⚡ `dentist-service.response.dto.ts` + `dentist-service.http-mapper.ts` (entidade → DTO; datas em ISO UTC).
      **DoD:** shape idêntico ao `DentistServicesResponse` do `openapi.yml`.
- [x] **T-05.3** `dentist-services.controller.ts`:
  - `@Controller('api/dentist-services')` e `@Auth(Role.Dentist)` **no nível da classe**;
  - `GET /`, `GET /:id`, `POST /`, `PUT /:id`, `DELETE /:id` (`@HttpCode(204)` no delete; 201 no POST);
  - `:id` com `ParseUUIDPipe`;
  - `@CurrentUser() user` → `user.id` vira `dentistId` nos inputs dos use cases;
  - controller fino: sem `try/catch`, sem lógica de negócio, só mapear DTO ⇄ use case;
  - Swagger: respostas documentadas conforme tabela do §7 (usando `ErrorResponseDto`).

  **DoD:** nenhum acesso direto a `fetch`, `jose` ou header `X-Dentist-Id` no controller.

- [x] **T-05.4** `dentist-services.module.ts`: provê `DENTIST_SERVICES_GATEWAY → HttpDentistServicesGateway`, os 5 use cases e o controller; importa o módulo de downstream HTTP.
      **DoD:** `AppModule` importa `AppConfigModule`, `AuthModule`, `DentistServicesModule`.

---

## F6 — Cross-cutting

- [x] **T-06.1** `DomainExceptionFilter` global (§9.3): `DomainError` → status por `code`; `HttpException` → `{ message }`; desconhecido → 500 genérico com log do stack.
      Manter o **mapa `code → status` em um único objeto** exportado.
      **DoD:** testes unitários para cada `code` da tabela 9.1 e para `HttpException` com `message` em array.
- [x] **T-06.2** `main.ts`: `helmet`, CORS por `CORS_ORIGINS`, `ValidationPipe` global (`whitelist: true`, `transform: true`), filtro global, `app.enableShutdownHooks()`.
      **DoD:** headers de segurança presentes; payload com campos extras é limpo.
- [x] **T-06.3** `CorrelationIdMiddleware`: lê/gera `X-Correlation-Id`, devolve na resposta e disponibiliza ao `DownstreamHttpClient` (via `AsyncLocalStorage`).
      **DoD:** o mesmo id aparece nos logs do BFF e no header enviado ao downstream.
- [x] **T-06.4** Logging com `nestjs-pino`: `redact` de `req.headers.authorization`; `correlationId` em todos os logs.
      **DoD:** nenhum log em nível algum contém token.
- [x] **T-06.5** `@nestjs/throttler` global.
      **DoD:** excesso retorna 429 no formato `{ message }`.
- [x] **T-06.6** `GET /health` com Terminus, marcado `@Public()`.
      **DoD:** 200 sem token.
- [x] **T-06.7** Swagger em `/docs` condicionado a `SWAGGER_ENABLED`, com `addBearerAuth()`.
      **DoD:** botão "Authorize" funcional; rotas de dentist mostram o cadeado.

---

## F7 — Testes

> Referência: §12 do `spec.md`. A tarefa **T-07.1 (test kit) deve ser feita logo após a F1**, para que cada fase seguinte já entregue seus testes junto com o código (os testes de F2–F6 são escritos nelas; aqui ficam a infraestrutura e a verificação final).

### Infraestrutura de testes

- [x] **T-07.1** Configurar Jest com 3 projects (`unit`, `integration`, `e2e`), scripts `test`, `test:unit`, `test:int`, `test:e2e`, `test:cov`, aliases `@shared/*` e `@modules/*`, e `coverageThreshold` do §12.10.
      **DoD:** cada script roda isolado; `test:cov` falha abaixo das metas.
- [x] **T-07.2** Criar o test kit em `test/support/` (§12.3):
  - [x] `test-config.ts` (`buildTestConfig`)
  - [x] `builders/` (`aDentistService`, `anAuthenticatedUser`)
  - [x] `fakes/fake-dentist-services.gateway.ts` (estado em memória, `calls`, `failNext`)
  - [x] `fakes/fake-token-verifier.ts`
  - [x] `jwt-test-kit.ts` (par RSA, servidor JWKS local em porta `0`, `signToken` com todos os claims sobrescrevíveis, rotação de chave)
  - [x] `downstream-mock.ts` (segue o `openapi.yml`; registra requisições; força 400/403/404/5xx/atraso/conexão recusada)

  **DoD:** o test kit é usado por unit, integração e E2E sem duplicar helpers; o fake do gateway tem seus próprios testes de sanidade (get/403/404).

- [ ] **T-07.3** Regras de lint de teste: `jest/no-focused-tests`, `jest/no-disabled-tests`, `jest/expect-expect`.
      **DoD:** `.only` e `.skip` quebram o lint.

### Testes unitários — domain

- [x] **T-07.4** Erros de domínio: `code` correto e herança de `DomainError` (todos da tabela §9.1).
      **DoD:** um teste parametrizado (`it.each`) cobre todas as classes.
- [x] **T-07.5** Enums `Role` e `DentistServiceStatus` com valores exatos (§12.5).
      **DoD:** teste falha se um valor for renomeado.

### Testes unitários — application

- [x] **T-07.6** `extractRoles`: 9 cenários do §12.4.
      **DoD:** todos os cenários existem como `it`; inclui case-sensitivity e `roles` não-array.
- [x] **T-07.7** `AuthenticateUseCase`: 6 cenários do §12.4.
      **DoD:** verificar que `verify` **não** é chamado quando o header é inválido.
- [x] **T-07.8** `ListDentistServicesUseCase`: 3 cenários.
- [x] **T-07.9** `GetDentistServiceByIdUseCase`: 3 cenários.
- [x] **T-07.10** `CreateDentistServiceUseCase`: 3 cenários (incluir os três valores de `status` com `it.each`).
- [x] **T-07.11** `UpdateDentistServiceUseCase`: 6 cenários.
      **DoD:** asserções explícitas de **ordem** (`getById` antes de `update`) e de que `update` nunca é chamado em 403/404.
- [x] **T-07.12** `DeleteDentistServiceUseCase`: 3 cenários.
      **DoD (T-07.8 a T-07.12):** cada linha das tabelas do §12.4 corresponde a um `it`; sem Nest e sem rede; cobertura de `application` ≥ 90% linhas / 85% branches.

### Testes unitários — adapters

- [x] **T-07.13** `DownstreamHttpClient`: 11 cenários do §12.6 (`fetch` mockado; timers falsos para timeout).
      **DoD:** nenhum `sleep` real.
- [x] **T-07.14** `HttpDentistServicesGateway`: método, URL, headers e body das 5 operações; tradução de 403/404; `id` codificado na URL.
- [x] **T-07.15** Mappers (downstream → entidade; entidade → DTO).
- [x] **T-07.16** `JwtAuthGuard` e `RolesGuard` (incluindo fail-closed, OU entre roles, precedência método > classe).
- [x] **T-07.17** `@Auth` (metadados via `Reflector`) e `DomainExceptionFilter` (cada `code`, `HttpException` com `message` array, erro desconhecido sem stack no corpo).
- [x] **T-07.18** Validação de DTOs com `ValidationPipe`: limites de `name` (2/3/100/101), `price` (0, negativo, decimal, string), `isPeriodic`, `status` inválido, `dentistId` descartado.
      **DoD:** `it.each` com tabela de entradas inválidas.

### Testes de integração

- [x] **T-07.19** `JoseJwksTokenVerifier` com `jwt-test-kit`: 13 cenários do §12.7 (inclui `alg=none`, confusão HS256/RS256, rotação de `kid` e JWKS fora do ar).
      **DoD:** todos os inválidos resultam em `InvalidTokenError`; spy de logger confirma que o token não é logado.
- [x] **T-07.20** `DownstreamHttpClient` contra servidor local: timeout real, conexão recusada, resposta truncada.
      **DoD:** tempo total da suíte de integração < 30 s.

### Testes E2E

> **PULADO nesta entrega por decisão do solicitante (12/10/2026).** Infra pronta: `configureApp(app)` em `src/main.ts`, `test/support/jwt-test-kit.ts` e `test/support/downstream-mock.ts`.

- [ ] **T-07.21** Bootstrap E2E: `AppModule` real + JWKS local + `downstream-mock` em `beforeAll`/`afterAll`, usando o mesmo `main.ts` setup (pipes, filtro, helmet) via função `configureApp(app)` compartilhada.
      **DoD:** E2E exercita exatamente a mesma configuração de produção (sem duplicar pipes/filtros no teste).
- [ ] **T-07.22** E2E — autenticação e RBAC (casos 1–8 do §12.8), incluindo o controller fictício sem `@Auth` para o fail-closed.
- [ ] **T-07.23** E2E — rotas do Dentist Services (casos 9–21): headers injetados, anti-spoofing de `dentistId`, fluxo `GET` → `PUT`, sem chamada ao downstream em 400/403/404 do PUT.
- [ ] **T-07.24** E2E — falhas do downstream (casos 22–26): 400, 502, 504 e formato `{ message }` em todas.
- [ ] **T-07.25** E2E — transversais: `X-Correlation-Id` (recebido, gerado, repassado), ausência de `Authorization` nos logs, 429 do throttler, headers do helmet.
      **DoD (T-07.22 a T-07.25):** cada linha das tabelas do §12.8 é um `it`; downstream mock confirma o que **não** foi chamado onde exigido.

### Arquitetura e portões de qualidade

- [x] **T-07.26** Teste de arquitetura: `npm run arch:check` (já criado em T-01.5) incluído em `npm test` e no CI.
      **DoD:** import proibido de teste faz a suíte falhar.
- [x] **T-07.27** Gate de cobertura (`coverageThreshold` do §12.10) e relatório `lcov` publicado como artefato do CI.
      **DoD:** domain ≥ 95/90, application ≥ 90/85, global ≥ 80/75.
- [ ] **T-07.28** Teste de mutação opcional (Stryker) restrito a `application/` e `role-extractor`.
      **DoD (opcional):** mutation score ≥ 80% nesses diretórios.

---

## F8 — Entrega

- [x] **T-08.1** `Dockerfile` multi-stage (node:20-alpine), usuário não root, `HEALTHCHECK` em `/health`.
      **DoD:** imagem sobe e responde `/health`.
- [x] **T-08.2** `docker-compose.dev.yml` com Keycloak (realm de dev importado com as roles `employ`, `dentist`, `pacient` e 3 usuários de teste), BFF e mock do Dentist Service.
      **DoD:** `docker compose up` permite obter token e chamar o BFF ponta a ponta.
- [x] **T-08.3** Pipeline CI: install → lint → `arch:check` → unit → e2e → build.
      **DoD:** pipeline verde.
- [x] **T-08.4** `README.md`: como subir, variáveis de ambiente, como obter token de teste, matriz RBAC, como adicionar um novo módulo (§14).
      **DoD:** um dev novo roda o projeto só com o README.

---

## Checklist final de aceitação

- [ ] Todas as rotas `/api/dentist-services` retornam **401** sem token válido.
- [ ] `pacient` e `employ` recebem **403**; `dentist` acessa.
- [ ] O cliente nunca envia `X-Dentist-Id` nem `dentistId`; o BFF usa o `sub` do token.
- [ ] Dentista A **não** consegue ler, alterar ou apagar serviço do dentista B (incluindo **PUT**).
- [ ] Erros sempre no formato `{ "message": string }`.
- [x] Nenhum `try/catch` de negócio em controllers; nenhum header/role como string solta; nenhuma lógica de auth fora do módulo `auth`.
- [x] `arch:check` verde (domain/application sem imports de infraestrutura).
- [ ] Adicionar um segundo módulo downstream não exige alterar `auth/` nem `shared/` (exceto novos erros no mapa).
- [x] Variáveis inválidas impedem o boot; token nunca aparece em logs.
- [ ] Todas as linhas das tabelas de cenários do §12 (application, adapters, integração, E2E) existem como testes.
- [x] Cobertura: domain ≥ 95/90, application ≥ 90/85, global ≥ 80/75 (linhas/branches).
- [x] Nenhum `.only`/`.skip` e nenhum `sleep` real nos testes.

---

## Backlog futuro (fora desta entrega)

- [ ] Módulos `pacient` e `employ` (novas rotas com `@Auth(Role.Pacient)` / `@Auth(Role.Employ)`).
- [ ] Atualizar o contrato do Dentist Service para exigir `X-Dentist-Id` no `PUT` e retornar 403 → remover o pré-check do `UpdateDentistServiceUseCase`.
- [ ] Geração de tipos do downstream a partir do `openapi.yml` (`openapi-typescript`) para validar o mapper em tempo de compilação.
- [ ] Cache curto de leituras e circuit breaker no `DownstreamHttpClient`.
- [ ] Rota separada para `employ` consultar serviços de qualquer dentista (decisão D-7).
