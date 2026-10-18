#!/usr/bin/env bash
# Cria a política ILM e o index template dos logs. Idempotente (PUT).
set -euo pipefail

ES_URL="${ES_URL:-http://elasticsearch:9200}"

put() {
  local path="$1" file="$2"
  echo ">> PUT ${path}"
  curl -fsS -X PUT "${ES_URL}${path}" \
    -H 'Content-Type: application/json' \
    --data-binary "@${file}"
  echo
}

put "/_ilm/policy/odonto-logs-policy" /setup/ilm-policy.json
put "/_index_template/odonto-logs" /setup/index-template.json

echo "Elasticsearch configurado."
