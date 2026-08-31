resource "newrelic_alert_policy" "processamento_os" {
  name                = "Oficina Mecanica - Falhas no processamento de OS"
  incident_preference = "PER_CONDITION"
}

resource "newrelic_nrql_alert_condition" "erros_integracao" {
  policy_id                    = newrelic_alert_policy.processamento_os.id
  name                         = "Erros de integracao (e-mail) acima do normal"
  description                  = "Dispara quando falhas ao notificar clientes (envio de e-mail durante uma mudanca de status de OS) excedem o limite na janela de 5 minutos."
  enabled                      = true
  violation_time_limit_seconds = 3600

  nrql {
    query = "SELECT count(*) FROM Metric WHERE metricName = 'integracoes.erros'"
  }

  critical {
    operator              = "above"
    threshold             = 5
    threshold_duration    = 300
    threshold_occurrences = "at_least_once"
  }

  warning {
    operator              = "above"
    threshold             = 1
    threshold_duration    = 300
    threshold_occurrences = "at_least_once"
  }
}

resource "newrelic_notification_destination" "email" {
  name = "oficina-mecanica-email"
  type = "EMAIL"

  property {
    key   = "email"
    value = var.notification_email
  }
}

resource "newrelic_notification_channel" "email" {
  name           = "oficina-mecanica-email-channel"
  type           = "EMAIL"
  destination_id = newrelic_notification_destination.email.id
  product        = "IINT"

  property {
    key   = "subject"
    value = "Oficina Mecanica - Alerta: {{ issueTitle }}"
  }
}

resource "newrelic_workflow" "processamento_os" {
  name                  = "Oficina Mecanica - Notificar falhas de processamento de OS"
  muting_rules_handling = "NOTIFY_ALL_ISSUES"

  issues_filter {
    name = "Filtrar por policy de processamento de OS"
    type = "FILTER"

    predicate {
      attribute = "labels.policyIds"
      operator  = "EXACTLY_MATCHES"
      values    = [newrelic_alert_policy.processamento_os.id]
    }
  }

  destination {
    channel_id = newrelic_notification_channel.email.id
  }
}
