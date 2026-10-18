#!/usr/bin/env bash
# Cria o data view dos logs e importa buscas/dashboard salvos no Kibana. Idempotente.
set -euo pipefail

KIBANA_URL="${KIBANA_URL:-http://kibana:5601}"
HDR=(-H 'kbn-xsrf: true')

echo ">> Criando data view odonto-logs (logs-*-dev)"
curl -fsS -X POST "${KIBANA_URL}/api/data_views/data_view" "${HDR[@]}" \
  -H 'Content-Type: application/json' \
  -d '{"override":true,"data_view":{"id":"odonto-logs","title":"logs-*-dev","name":"Odontologia - logs","timeFieldName":"@timestamp"}}' \
  >/dev/null
echo

echo ">> Importando buscas e dashboard"
curl -fsS -X POST "${KIBANA_URL}/api/saved_objects/_import?overwrite=true" "${HDR[@]}" \
  --form file=@/setup/saved-objects.ndjson
echo

echo "Kibana configurado."
