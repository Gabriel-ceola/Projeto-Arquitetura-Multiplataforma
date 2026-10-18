#!/usr/bin/env bash
#
# Seeder idempotente do Keycloak do projeto.
#
# Garante, com o Keycloak já em execução, que existam:
#   - o realm `dentist`
#   - os clientes `dentist-app` (público) e `dentist-api`
#   - as roles de realm `dentist`, `employ` e `patient`
#   - os usuários de teste `dentista1`, `funcionario1` e `paciente1`
#   - o accessTokenLifespan do realm em 12h (evita expiração nos testes)
#
# Pode ser executado quantas vezes quiser: só cria o que estiver faltando.
# Não apaga nem sobrescreve dados existentes (a senha é redefinida para o
# valor padrão e as roles são (re)atribuídas).
#
# Uso:
#   ./keycloak/seed.sh
#
# Variáveis de ambiente opcionais:
#   KC_SERVER           (padrão: http://localhost:8080)
#   KC_ADMIN_USER       (padrão: admin)
#   KC_ADMIN_PASSWORD   (padrão: admin)
#   REALM               (padrão: dentist)
#   DEFAULT_PASSWORD    (padrão: 123456)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.yaml"
SERVICE="keycloak"
KCADM="/opt/keycloak/bin/kcadm.sh"
KCADM_CONFIG="/tmp/kcadm.config"

KC_SERVER="${KC_SERVER:-http://localhost:8080}"
KC_ADMIN_USER="${KC_ADMIN_USER:-admin}"
KC_ADMIN_PASSWORD="${KC_ADMIN_PASSWORD:-admin}"
REALM="${REALM:-dentist}"
DEFAULT_PASSWORD="${DEFAULT_PASSWORD:-123456}"
ACCESS_TOKEN_LIFESPAN="${ACCESS_TOKEN_LIFESPAN:-43200}"   # 12h — conveniência para testes manuais

# O `--config` precisa vir DEPOIS do comando neste kcadm.sh.
kc() {
  docker compose -f "$COMPOSE_FILE" exec -T "$SERVICE" "$KCADM" "$@" --config "$KCADM_CONFIG"
}

log() { printf '  %s\n' "$*"; }

# ─────────────────────────────────────────────────────────────
# Pré-requisitos
# ─────────────────────────────────────────────────────────────

if ! docker compose -f "$COMPOSE_FILE" ps --status running --services 2>/dev/null | grep -qx "$SERVICE"; then
  echo "ERRO: o serviço '$SERVICE' não está em execução." >&2
  echo "Suba a infraestrutura com: docker compose -f keycloak/docker-compose.yaml up -d" >&2
  exit 1
fi

echo "Autenticando no Keycloak ($KC_SERVER) como '$KC_ADMIN_USER'..."
kc config credentials \
  --server "$KC_SERVER" \
  --realm master \
  --user "$KC_ADMIN_USER" \
  --password "$KC_ADMIN_PASSWORD" >/dev/null

# ─────────────────────────────────────────────────────────────
# Realm
# ─────────────────────────────────────────────────────────────

if kc get "realms/$REALM" >/dev/null 2>&1; then
  log "realm '$REALM' já existe"
else
  kc create realms -s "realm=$REALM" -s enabled=true >/dev/null
  log "realm '$REALM' criado"
fi

# Duração do access token (12h por padrão) — evita expiração durante testes manuais.
# O ssoSessionMaxLifespan precisa ser >= ao accessTokenLifespan, pois limita a validade efetiva.
kc update "realms/$REALM" \
  -s "accessTokenLifespan=$ACCESS_TOKEN_LIFESPAN" \
  -s "ssoSessionMaxLifespan=$ACCESS_TOKEN_LIFESPAN" >/dev/null
log "accessTokenLifespan e ssoSessionMaxLifespan do realm '$REALM' definidos para ${ACCESS_TOKEN_LIFESPAN}s"

# ─────────────────────────────────────────────────────────────
# Clientes
# ─────────────────────────────────────────────────────────────

ensure_client() {
  local client_id="$1"; shift
  if kc get clients -r "$REALM" -q "clientId=$client_id" --fields clientId 2>/dev/null | grep -q '"clientId"'; then
    log "cliente '$client_id' já existe"
  else
    kc create clients -r "$REALM" -s "clientId=$client_id" "$@" >/dev/null
    log "cliente '$client_id' criado"
  fi
}

ensure_client dentist-app \
  -s name="Dentist App (Swagger/Front/Postman)" \
  -s enabled=true \
  -s publicClient=true \
  -s standardFlowEnabled=true \
  -s directAccessGrantsEnabled=true \
  -s 'redirectUris=["http://localhost:5068/*","https://localhost:7235/*","http://localhost:3000/*","http://localhost:4200/*"]' \
  -s 'webOrigins=["+"]' \
  -s 'attributes={"pkce.code.challenge.method":"S256"}'

ensure_client dentist-api \
  -s name="Dentist Service API" \
  -s enabled=true \
  -s publicClient=false \
  -s standardFlowEnabled=false \
  -s directAccessGrantsEnabled=false \
  -s serviceAccountsEnabled=false

# ─────────────────────────────────────────────────────────────
# Roles
# ─────────────────────────────────────────────────────────────

ensure_role() {
  local role="$1" desc="$2"
  if kc get "roles/$role" -r "$REALM" >/dev/null 2>&1; then
    log "role '$role' já existe"
  else
    kc create roles -r "$REALM" -s "name=$role" -s "description=$desc" >/dev/null
    log "role '$role' criada"
  fi
}

ensure_role dentist "Dentista dono de serviços"
ensure_role employ "Funcionário da clínica (gestão de funcionários)"
ensure_role patient "Paciente que solicita agendamentos"

# ─────────────────────────────────────────────────────────────
# Usuários
# ─────────────────────────────────────────────────────────────

ensure_user() {
  local username="$1" email="$2" first="$3" last="$4" role="$5"
  if kc get users -r "$REALM" -q "username=$username" --fields username 2>/dev/null | grep -q '"username"'; then
    log "usuário '$username' já existe"
  else
    kc create users -r "$REALM" \
      -s "username=$username" \
      -s "email=$email" \
      -s enabled=true \
      -s emailVerified=true \
      -s "firstName=$first" \
      -s "lastName=$last" >/dev/null
    log "usuário '$username' criado"
  fi
  kc set-password -r "$REALM" --username "$username" --new-password "$DEFAULT_PASSWORD" >/dev/null
  kc add-roles -r "$REALM" --uusername "$username" --rolename "$role" >/dev/null
}

ensure_user dentista1    dentista1@teste.com    Dentista   Teste dentist
ensure_user funcionario1 funcionario1@teste.com Funcionario Teste employ
ensure_user paciente1    paciente1@teste.com    Paciente   Teste patient

echo
echo "Seed concluído. Usuários de teste (senha '$DEFAULT_PASSWORD'):"
echo "  dentista1    -> role dentist"
echo "  funcionario1 -> role employ"
echo "  paciente1    -> role patient"
