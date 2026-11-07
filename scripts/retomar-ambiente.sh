#!/usr/bin/env bash
# Retoma o ambiente depois que a sessao do AWS Academy Learner Lab expira.
#
# Pre-requisito: Start Lab no AWS Academy e colar as credenciais novas em
# ~/.aws/credentials (AWS Details > AWS CLI > Show).
#
# Uso: bash scripts/retomar-ambiente.sh
set -euo pipefail

OWNER=konrath9
IP_ESPERADO=32.193.30.44                     # conta do Learner Lab da Fase 4 (897966076873)
SERVICOS="OficinaMecanica oficina-billing-service oficina-execution-service"

echo "== 1. credenciais locais =="
aws sts get-caller-identity --query Account --output text

echo "== 2. propagando credenciais para os repos de infra =="
AK=$(aws configure get aws_access_key_id)
SK=$(aws configure get aws_secret_access_key)
ST=$(aws configure get aws_session_token)
for r in oficina-mecanica-infra-db oficina-mecanica-infra-k8s oficina-mecanica-auth; do
  gh secret set AWS_ACCESS_KEY_ID     -R "$OWNER/$r" --body "$AK"
  gh secret set AWS_SECRET_ACCESS_KEY -R "$OWNER/$r" --body "$SK"
  gh secret set AWS_SESSION_TOKEN     -R "$OWNER/$r" --body "$ST"
  echo "  $r ok"
done

echo "== 3. estado da infra =="
INSTANCIA=$(aws ec2 describe-instances \
  --filters "Name=tag:Name,Values=oficina-mecanica-k3s" "Name=instance-state-name,Values=running,stopped" \
  --query 'Reservations[].Instances[].[InstanceId,State.Name,PublicIpAddress]' --output text)
echo "  ec2: ${INSTANCIA:-AUSENTE}"
echo "  rds: $(aws rds describe-db-instances --db-instance-identifier oficina-mecanica-db \
  --query 'DBInstances[0].DBInstanceStatus' --output text 2>/dev/null || echo AUSENTE)"

# Instancia parada: liga e espera. Ausente: precisa reaplicar o terraform.
if [[ "$INSTANCIA" == *stopped* ]]; then
  ID=$(echo "$INSTANCIA" | awk '{print $1}')
  echo "  ligando $ID..."
  aws ec2 start-instances --instance-ids "$ID" >/dev/null
  aws ec2 wait instance-running --instance-ids "$ID"
elif [[ -z "$INSTANCIA" ]]; then
  echo "  !! EC2 nao existe. Reaplique na ordem: infra-db -> infra-k8s -> auth"
  echo "     (rode 'terraform apply' em cada repo, ou 'gh run rerun <id>' do ultimo run)"
  exit 1
fi

IP=$(aws ec2 describe-instances \
  --filters "Name=tag:Name,Values=oficina-mecanica-k3s" "Name=instance-state-name,Values=running" \
  --query 'Reservations[].Instances[].PublicIpAddress' --output text)
echo "  ip atual: $IP"

if [[ "$IP" != "$IP_ESPERADO" ]]; then
  echo "  !! IP mudou. Atualize tambem:"
  echo "     for r in $SERVICOS; do gh secret set K3S_HOST -R $OWNER/\$r --body '$IP'; done"
  echo "     gh secret set APP_PUBLIC_IP -R $OWNER/oficina-mecanica-auth --body '$IP'"
  echo "     terraform apply -var=\"app_host=$IP\" ... em observability/newrelic"
  echo "     e o IP_ESPERADO no topo deste script"
fi

echo "== 4. k3s respondendo? =="
# Traefik pode levar ~1-2 min depois do boot; 404 dele ja e sinal de vida.
for i in $(seq 1 12); do
  CODE=$(curl -s -o /dev/null -m 10 -w '%{http_code}' "http://$IP/" || true)
  [[ "$CODE" != "000" ]] && break
  sleep 10
done
echo "  http://$IP/ -> $CODE" 

echo "== 5. redeploy dos 3 microsservicos =="
gh workflow run ci-cd.yml -R "$OWNER/OficinaMecanica" --ref master
gh workflow run ci-cd.yml -R "$OWNER/oficina-billing-service" --ref main
gh workflow run ci-cd.yml -R "$OWNER/oficina-execution-service" --ref main
echo "  disparados. acompanhe: gh run watch -R $OWNER/<repo>"
echo
echo "Valide depois com:"
echo "  curl -s http://$IP/health"
echo "  OS_URL=http://$IP BILLING_URL=http://$IP EXECUTION_URL=http://$IP node scripts/demo-saga.mjs"
echo "  curl -X POST https://vmdccz407g.execute-api.us-east-1.amazonaws.com/auth/login \\"
echo "    -H 'Content-Type: application/json' -d '{\"cpf\":\"74185296355\"}'"
