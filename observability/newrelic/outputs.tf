output "dashboard_url" {
  value       = newrelic_one_dashboard.oficina_mecanica.permalink
  description = "Link direto para o dashboard no New Relic"
}

output "alert_policy_id" {
  value       = newrelic_alert_policy.processamento_os.id
  description = "Id da policy de alerta de falhas no processamento de OS"
}
