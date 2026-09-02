# Monitor de uptime/healthcheck (New Relic Synthetics) - pinga o /health da API a cada 5 min.
resource "newrelic_synthetics_monitor" "healthcheck" {
  name             = "Oficina Mecanica - Healthcheck"
  type             = "SIMPLE"
  locations_public = ["AWS_US_EAST_1"]
  period           = "EVERY_5_MINUTES"
  status           = "ENABLED"
  uri              = "http://${var.app_host}/health"

  treat_redirect_as_failure = false
  validation_string         = "Healthy"
  verify_ssl                = false
}

resource "newrelic_alert_policy" "uptime" {
  name                = "Oficina Mecanica - Uptime"
  incident_preference = "PER_CONDITION"
}

resource "newrelic_nrql_alert_condition" "healthcheck_falhando" {
  policy_id                    = newrelic_alert_policy.uptime.id
  name                         = "Healthcheck fora do ar"
  description                  = "Dispara quando o monitor de Synthetics reporta falha no /health."
  enabled                      = true
  violation_time_limit_seconds = 3600

  nrql {
    query = "SELECT count(*) FROM SyntheticCheck WHERE monitorName = '${newrelic_synthetics_monitor.healthcheck.name}' AND result = 'FAILED'"
  }

  critical {
    operator              = "above"
    threshold             = 0
    threshold_duration    = 300
    threshold_occurrences = "at_least_once"
  }
}

resource "newrelic_workflow" "uptime" {
  name                  = "Oficina Mecanica - Notificar healthcheck fora do ar"
  muting_rules_handling = "NOTIFY_ALL_ISSUES"

  issues_filter {
    name = "Filtrar por policy de uptime"
    type = "FILTER"

    predicate {
      attribute = "labels.policyIds"
      operator  = "EXACTLY_MATCHES"
      values    = [newrelic_alert_policy.uptime.id]
    }
  }

  destination {
    channel_id = newrelic_notification_channel.email.id
  }
}
