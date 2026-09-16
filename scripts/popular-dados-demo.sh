#!/usr/bin/env bash
# Popula o ambiente com Ordens de Servico percorrendo o ciclo de vida completo,
# para que os paineis de negocio do dashboard (volume diario, tempo medio por
# status, erros de integracao) tenham dados antes da gravacao do video.
#
# Uso: bash scripts/popular-dados-demo.sh [host] [quantidade]
#   host       default 98.82.219.28 (IP do k3s, mesmo valor do secret K3S_HOST)
#   quantidade default 5 (quantas OS criar)
set -euo pipefail

HOST=${1:-98.82.219.28}
QTD=${2:-5}
BASE="http://$HOST/api"

EMAIL=demo@oficina.com
SENHA='Demo@123456'

# IDs fixos do seed (SeedData.cs) - existem desde a primeira migration.
CLIENTES=(a1b2c3d4-e5f6-7890-abcd-ef1234567801 a1b2c3d4-e5f6-7890-abcd-ef1234567802 a1b2c3d4-e5f6-7890-abcd-ef1234567803)
VEICULOS=(b2c3d4e5-f6a7-8901-bcde-f12345678901 b2c3d4e5-f6a7-8901-bcde-f12345678902 b2c3d4e5-f6a7-8901-bcde-f12345678903)
SERVICOS=(c3d4e5f6-a7b8-9012-cdef-123456789001 c3d4e5f6-a7b8-9012-cdef-123456789002 c3d4e5f6-a7b8-9012-cdef-123456789004)
PECAS=(d4e5f6a7-b8c9-0123-defa-234567890001 d4e5f6a7-b8c9-0123-defa-234567890002 d4e5f6a7-b8c9-0123-defa-234567890003)

json() { sed -n "s/.*\"$2\":\"\?\([^,\"}]*\)\"\?.*/\1/p" <<<"$1"; }

echo "== login staff =="
# Endpoint de registro e publico; ignora erro se o usuario ja existir.
curl -s -o /dev/null -X POST "$BASE/autenticacao/registrar" -H 'Content-Type: application/json' \
  -d "{\"nome\":\"Demo Admin\",\"email\":\"$EMAIL\",\"senha\":\"$SENHA\",\"perfil\":1}" || true

TOKEN=$(json "$(curl -s -X POST "$BASE/autenticacao/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"senha\":\"$SENHA\"}")" token)
[[ -n "$TOKEN" ]] || { echo "falha no login - a API respondeu sem token"; exit 1; }
AUTH=(-H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json')
echo "  ok"

for i in $(seq 1 "$QTD"); do
  n=$(( (i - 1) % 3 ))
  CLI=${CLIENTES[$n]}; VEI=${VEICULOS[$n]}; SRV=${SERVICOS[$n]}; PEC=${PECAS[$n]}

  OS=$(curl -s -X POST "$BASE/ordens-servico" "${AUTH[@]}" \
    -d "{\"clienteId\":\"$CLI\",\"veiculoId\":\"$VEI\",\"observacoes\":\"OS de demonstracao $i\"}")
  ID=$(json "$OS" id)
  [[ -n "$ID" ]] || { echo "[$i] falha ao criar OS: $OS"; continue; }

  curl -s -o /dev/null -X POST "$BASE/ordens-servico/$ID/servicos" "${AUTH[@]}" -d "{\"servicoId\":\"$SRV\",\"quantidade\":1}"
  curl -s -o /dev/null -X POST "$BASE/ordens-servico/$ID/pecas"    "${AUTH[@]}" -d "{\"pecaId\":\"$PEC\",\"quantidade\":2}"

  # Cada transicao vira um ponto no histograma tempo_por_status_segundos.
  # O sleep separa os timestamps para o "tempo medio por status" nao ficar ~0.
  curl -s -o /dev/null -X POST "$BASE/ordens-servico/$ID/concluir-diagnostico" "${AUTH[@]}"
  sleep 2

  NUM=$(json "$(curl -s "$BASE/ordens-servico/$ID" "${AUTH[@]}")" numero)
  curl -s -o /dev/null -X POST "$BASE/acompanhamento/$NUM/aprovar" "${AUTH[@]}"
  sleep 2

  curl -s -o /dev/null -X PUT "$BASE/ordens-servico/$ID/servicos/$SRV/execucao" "${AUTH[@]}" -d '{"acao":"iniciar"}'
  curl -s -o /dev/null -X PUT "$BASE/ordens-servico/$ID/servicos/$SRV/execucao" "${AUTH[@]}" -d '{"acao":"finalizar"}'
  sleep 2

  ENTREGA=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/ordens-servico/$ID/registrar-entrega" "${AUTH[@]}")
  echo "[$i] $NUM -> entregue (HTTP $ENTREGA)"
done

echo
echo "== trafego extra para os paineis de latencia =="
for _ in $(seq 1 20); do
  curl -s -o /dev/null "$BASE/ordens-servico" "${AUTH[@]}"
  curl -s -o /dev/null "$BASE/clientes" "${AUTH[@]}"
  curl -s -o /dev/null "http://$HOST/health"
done
echo "  ok"

echo
echo "Dados no New Relic em ~1-2 min. Confira com:"
echo "  SELECT count(*) FROM Metric WHERE metricName = 'ordens_servico.criadas' SINCE 1 hour ago"
echo "  SELECT average(ordens_servico.tempo_por_status_segundos) FROM Metric FACET status SINCE 1 hour ago"
