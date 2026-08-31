# Observabilidade — New Relic

Terraform que provisiona os dashboards e o alerta exigidos pela Fase 3, via
[provider `newrelic/newrelic`](https://registry.terraform.io/providers/newrelic/newrelic/latest/docs).
A aplicação já instrumenta o que esses recursos consultam — ver [`../../docs/adr/`](../../docs/) e a
seção "Observabilidade" do README principal.

## Pré-requisitos

1. Conta [New Relic](https://newrelic.com/signup) (free tier — 100GB/mês de ingestão, sem cartão de crédito).
2. **License Key** (Ingest - License): usada pela aplicação para enviar telemetria via OTLP. Em `one.newrelic.com` → **API keys** → filtrar por "Ingest - License".
3. **User API Key** (`NRAK-...`): usada só pelo Terraform aqui para criar dashboard/alerta. Mesma tela, filtrar por "User".
4. **Account ID**: visível na mesma tela de API keys, ou na URL do New Relic (`one.newrelic.com/.../accounts/<ID>/...`).

## O que este Terraform cria

- `newrelic_one_dashboard` — 1 dashboard com 4 widgets: volume diário de OS, tempo médio de execução por status (Diagnóstico/Execução/Finalização), erros de integração, e latência das APIs (p50/p95 por rota).
- `newrelic_alert_policy` + `newrelic_nrql_alert_condition` — alerta quando falhas de integração (ex.: envio de e-mail durante o processamento de uma OS) excedem o limite em 5 minutos.
- `newrelic_notification_destination`/`newrelic_notification_channel`/`newrelic_workflow` — envia o alerta por e-mail.

## Como rodar

```bash
cd observability/newrelic
terraform init
terraform apply \
  -var="newrelic_account_id=<seu account id>" \
  -var="newrelic_api_key=<seu User API Key>" \
  -var="notification_email=<seu e-mail>"
```

## Ligando a aplicação ao New Relic

A aplicação só envia telemetria (traces + métricas via OTLP) se a variável de configuração
`NewRelic__LicenseKey` estiver definida — sem ela, a aplicação roda normalmente, sem tentar
exportar nada (é assim que os testes automatizados e o CI/CD de validação rodam hoje, sem
license key configurada).

Em produção, adicione `NewRelic__LicenseKey` (a **License Key**, não o User API Key) ao Secret
gerado pelo job `deploy-producao` do `OficinaMecanica` (ver o README principal — o job monta o
Secret de produção a partir de secrets do GitHub; `NEW_RELIC_LICENSE_KEY` deve ser adicionado à
mesma lista de secrets do repositório, junto com `PROD_DB_CONNECTION_STRING` e
`PROD_JWT_SECRET_KEY`). Como `k8s/deployment.yaml` já usa `envFrom: secretRef` para todo o
Secret, nenhuma mudança de manifesto é necessária — a variável passa a existir automaticamente
assim que estiver no Secret.
