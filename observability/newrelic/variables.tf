variable "newrelic_account_id" {
  description = "Id da conta New Relic (visivel na URL do dashboard ou em one.newrelic.com > perfil)"
  type        = string
}

variable "newrelic_api_key" {
  description = "User API Key do New Relic (NRAK-...), usada pelo provider Terraform para criar dashboards/alertas. Diferente da license key usada pela aplicacao para enviar telemetria via OTLP."
  type        = string
  sensitive   = true
}

variable "newrelic_region" {
  description = "Regiao da conta New Relic (US ou EU)"
  type        = string
  default     = "US"
}

variable "notification_email" {
  description = "E-mail que recebe os alertas de falha no processamento de OS"
  type        = string
}

variable "app_host" {
  description = "Host publico da aplicacao (IP do k3s, mesmo valor do secret K3S_HOST do repositorio principal) - usado pelo monitor de uptime/healthcheck"
  type        = string
}
