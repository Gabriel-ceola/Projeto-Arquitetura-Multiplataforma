# Odontologia Integrada — Projeto Multiplataforma

Monorepo da plataforma de gestão odontológica. Reúne o **BFF** (NestJS), três APIs
**.NET 10**, uma biblioteca de **mensageria Kafka** compartilhada e a **infraestrutura**
em Docker (Kafka, Keycloak e bancos PostgreSQL).

Cada serviço tem o seu próprio `docker-compose` e o seu próprio README com detalhes
internos. Este documento é o guia **de ponta a ponta**: como subir a infraestrutura e
todas as aplicações.

---

## Arquitetura

```
                         ┌──────────────────────────────┐
       Clientes          │   BFF (NestJS)  :3000        │
   (web / mobile) ─────▶ │  JWT Keycloak + RBAC         │
                         └───────┬───────────────┬──────┘
                                 │               │
                 ┌───────────────▼──┐      ┌─────▼────────────────┐
                 │ Dentist-service  │      │ Employee-service     │
                 │ .NET 10  :5068   │      │ .NET 10      :5070   │
                 └───┬──────────┬───┘      └──────────┬───────────┘
                     │          │                     │
        publica/consome│      │ EF Core             │ EF Core
                     │          ▼                     ▼
        ┌────────────▼───┐  ┌────────────┐      ┌────────────┐
        │ Kafka   :9092  │  │ Postgres   │      │ Postgres   │
        │ (KRaft) :8081  │  │ dentist    │      │ employee   │
        │ Kafka UI       │  │ :5432      │      │ :5434      │
        └────────────────┘  └────────────┘      └────────────┘

        ┌────────────────┐  ┌────────────┐      ┌────────────────┐
        │ Keycloak :8080 │  │ Postgres   │      │ PaymentService │
        │ realm dentist  │  │ keycloak   │      │ .NET 10 :5127  │
        └────────────────┘  │ :5435      │      └───────┬────────┘
                            └────────────┘              ▼
                                                  ┌────────────┐
                                                  │ Postgres   │
                                                  │ payment    │
                                                  │ :5433      │
                                                  └────────────┘
```

| Componente | Stack | Pasta |
|---|---|---|
| BFF | NestJS 11 / TypeScript / Node 20 (pnpm) | `bff/` |
| Dentist-service | .NET 10 / EF Core 10 / PostgreSQL | `Dentist-Service/Dentist-service/` |
| Employee-service | .NET 10 / EF Core 10 / PostgreSQL | `Employee-service/Employee-service/` |
| PaymentService | .NET 10 / EF Core 10 / PostgreSQL | `PaymentService/` |
| Messaging | Biblioteca .NET (Kafka + outbox) | `Messaging/` |
| Kafka | Apache Kafka 4.3.1 (KRaft) + Kafka UI | `kafka/` |
| Keycloak | Keycloak 26 + PostgreSQL | `keycloak/` |

---

## Pré-requisitos

| Ferramenta | Versão | Para quê |
|---|---|---|
| [Docker](https://www.docker.com/) + Docker Compose v2 | recente | Kafka, Keycloak e bancos |
| [.NET SDK](https://dotnet.microsoft.com/download) | **10** | as três APIs |
| [Node.js](https://nodejs.org/) + [pnpm](https://pnpm.io/) | **20** + **9** | BFF (`corepack enable`) |
| [EF Core CLI](https://learn.microsoft.com/ef/core/cli/dotnet) | 10.x | migrations (`dotnet tool install --global dotnet-ef`) |

> No Windows, os comandos com caminho `/opt/kafka/...` precisam de `MSYS_NO_PATHCONV=1` no Git Bash.

---

## Mapa de portas

| Serviço | Host | Observações |
|---|---|---|
| BFF | **3000** | Swagger em `/docs` se `SWAGGER_ENABLED=true` |
| Dentist-service | **5068** | HTTPS `7235`; OpenAPI em `/openapi/v1.json` |
| Employee-service | **5070** | HTTPS `7237`; OpenAPI em `/openapi/v1.json` |
| PaymentService | **5127** | HTTPS `7094`; Swagger só em Development |
| Keycloak | **8080** | realm `dentist` |
| Kafka UI | **8081** | kafbat UI |
| Kafka broker | **9092** | containers na rede usam `kafka:19092` |
| Postgres Dentist | **5432** | db `dentist_service` |
| Postgres Payment | **5433** | db `payment_service` |
| Postgres Employee | **5434** | db `employee_service` |
| Postgres Keycloak | **5435** | db `keycloak` |

Todas as senhas de desenvolvimento são `postgres` (bancos) e o Kafka é `PLAINTEXT`.

---

## Subindo a infraestrutura (Docker)

Cada pasta tem o seu próprio compose. Rode da **raiz do repositório** com `-f`:

```bash
# 1. Kafka + Kafka UI + criação dos tópicos
docker compose -f kafka/docker-compose.yaml up -d

# 2. Keycloak + banco do Keycloak (realm `dentist` importado automaticamente)
docker compose -f keycloak/docker-compose.yaml up -d

# 3. Bancos das APIs .NET
docker compose -f Dentist-Service/Dentist-service/docker-compose.yml up -d   # :5432
docker compose -f Employee-service/Employee-service/docker-compose.yml up -d # :5434
docker compose -f PaymentService/docker-compose.yml up -d                    # :5433
```

### Kafka

O `kafka-init` cria os tópicos e termina. A criação automática de tópicos está
desligada (`KAFKA_AUTO_CREATE_TOPICS_ENABLE=false`), então tópicos novos precisam
entrar no comando do `kafka-init`.

Tópicos: `appointments`, `payments`, `health-insurance` e as respectivas `.dlq`.

- Aplicações no host → `localhost:9092`
- Containers na rede `odontologia-network` → `kafka:19092`
- Kafka UI → `http://localhost:8081`

Detalhes de evento, envelope e DLQ em [`kafka/README.md`](kafka/README.md).

### Keycloak

- Console admin: `http://localhost:8080` — usuário `admin`, senha `admin`
- Realm: **`dentist`** (importado de `keycloak/keycloak/dentist-realm.json`)
- Roles de realm: `dentist`, `employ`, `patient`
- Usuários de teste (senha `123456`):

  | Usuário | Role | Acessa no BFF |
  |---|---|---|
  | `dentista1` | `dentist` | `/api/dentist-services` |
  | `funcionario1` | `employ` | `/api/employees` |
  | `paciente1` | `patient` | `/api/appointments` |

> O Keycloak só importa o realm quando ele ainda não existe. Se mudou o
> `dentist-realm.json` (ou já tinha subido antes), recrie o banco para reimportar:
> `docker compose -f keycloak/docker-compose.yaml down -v && docker compose -f keycloak/docker-compose.yaml up -d`.

#### Seeder

Com o Keycloak no ar, o `keycloak/seed.sh` garante — de forma **idempotente**, sem
apagar dados — o realm `dentist`, os clientes, as roles (`dentist`, `employ`,
`patient`) e os usuários de teste acima:

```bash
./keycloak/seed.sh
```

É a alternativa ao `down -v` para aplicar mudanças no realm com o Keycloak já
inicializado. Variáveis opcionais: `KC_SERVER`, `KC_ADMIN_USER`, `KC_ADMIN_PASSWORD`,
`REALM`, `DEFAULT_PASSWORD`.

Obter um token de teste:

```bash
curl -X POST "http://localhost:8080/realms/dentist/protocol/openid-connect/token" \
  -d "grant_type=password&client_id=dentist-app&username=funcionario1&password=123456"
```

---

## Subindo as aplicações

Suba sempre nesta ordem: **Kafka → banco → migrations → API**.

### Dentist-service (`:5068`)

```bash
cd Dentist-Service/Dentist-service
dotnet tool install --global dotnet-ef   # apenas na primeira vez
dotnet ef database update                # aplica as migrations
dotnet run                               # http://localhost:5068
```

Publica eventos em `appointments` e consome `payments` e `health-insurance`. Para
subir sem Kafka, use `"Kafka": { "Enabled": false }` no `appsettings.Development.json`.

### Employee-service (`:5070`)

```bash
cd Employee-service/Employee-service
dotnet ef database update
dotnet run                               # http://localhost:5070
```

### PaymentService (`:5127`)

```bash
cd PaymentService
dotnet run                               # http://localhost:5127
```

O `Program.cs` aplica as migrations automaticamente no boot — não precisa de
`dotnet ef database update`.

### BFF (`:3000`)

```bash
cd bff
cp .env.example .env                     # ajuste KEYCLOAK_* e DENTIST_SERVICE_BASE_URL
pnpm install
pnpm run start:dev                       # http://localhost:3000
```

Para rodar o BFF **no host** apontando para os serviços locais, o `.env` deve conter:

```bash
KEYCLOAK_ISSUER=http://localhost:8080/realms/dentist
KEYCLOAK_JWKS_URI=http://localhost:8080/realms/dentist/protocol/openid-connect/certs
KEYCLOAK_CLIENT_ID=dentist-app
DENTIST_SERVICE_BASE_URL=http://localhost:5068
EMPLOYEE_SERVICE_BASE_URL=http://localhost:5070
SWAGGER_ENABLED=true
```

Via Docker (sobe o BFF com um **mock** do Dentist-service, sem precisar da API real):

```bash
cd bff
docker compose -f docker-compose.dev.yml up
```

Lista completa de variáveis e a matriz de RBAC em [`bff/README.md`](bff/README.md).

---

## Resumo: subir o sistema inteiro

```bash
# Infraestrutura
docker compose -f kafka/docker-compose.yaml up -d
docker compose -f keycloak/docker-compose.yaml up -d
docker compose -f Dentist-Service/Dentist-service/docker-compose.yml up -d
docker compose -f Employee-service/Employee-service/docker-compose.yml up -d
docker compose -f PaymentService/docker-compose.yml up -d

# Migrations das APIs que não auto-migram
(cd Dentist-Service/Dentist-service && dotnet ef database update)
(cd Employee-service/Employee-service && dotnet ef database update)

# Aplicações (uma por terminal)
(cd Dentist-Service/Dentist-service && dotnet run)   # :5068
(cd Employee-service/Employee-service && dotnet run)  # :5070
(cd PaymentService && dotnet run)                     # :5127
(cd bff && pnpm run start:dev)                        # :3000
```

---

## Testes

```bash
# BFF
cd bff && pnpm run test:unit && pnpm run test:int

# Dentist-service
dotnet test Dentist-Service/Dentist-service.sln

# Employee-service
dotnet test Employee-service/Employee-service.sln

# Messaging
dotnet test Messaging/Messaging.Tests/Messaging.Tests.csproj
```

Os testes do BFF usam um JWKS local (`test/support/jwt-test-kit.ts`) — não exigem
um Keycloak real.

---

## Parando / limpando

```bash
docker compose -f kafka/docker-compose.yaml down            # add -v para apagar tópicos/dados
docker compose -f keycloak/docker-compose.yaml down -v      # remove o banco do Keycloak (reimporta o realm no próximo up)
docker compose -f Dentist-Service/Dentist-service/docker-compose.yml down -v
docker compose -f Employee-service/Employee-service/docker-compose.yml down -v
docker compose -f PaymentService/docker-compose.yml down -v
```

---

## Troubleshooting

- **Conflito de portas nos bancos.** Cada banco publica uma porta diferente no host:
  Dentist `5432`, Payment `5433`, Employee `5434`, Keycloak `5435`. O Postgres do
  Keycloak fica em `5435` justamente porque `5433` é do PaymentService. Se aparecer
  `port is already allocated`, ajuste a porta publicada no compose correspondente.
- **Realm do Keycloak desatualizado.** O realm só é importado na primeira subida.
  Alterou o JSON? `docker compose -f keycloak/docker-compose.yaml down -v && up -d`.
- **`dotnet ef` não encontrado.** `dotnet tool install --global dotnet-ef` e garanta
  que `~/.dotnet/tools` está no `PATH`.
- **Dentist-service com erro 500 ao criar agendamento.** Falta aplicar as migrations
  (a tabela `outbox_messages` precisa existir): `dotnet ef database update`.
- **BFF não sobe.** O boot valida o `.env` e falha listando as variáveis inválidas.
  Confira `KEYCLOAK_ISSUER`, `KEYCLOAK_JWKS_URI`, `KEYCLOAK_CLIENT_ID` e
  `DENTIST_SERVICE_BASE_URL`.

---

## Observações

- O `PaymentService` ainda não tem README, solução (`.sln`) nem testes, e publica
  eventos apenas via log (sem Kafka).
- Pastas `Dentist-service.Tests/` (na raiz) e `Dentist-Service/service-Dentist/`
  contêm apenas artefatos de build (`bin/`/`obj/`), sem código-fonte.
