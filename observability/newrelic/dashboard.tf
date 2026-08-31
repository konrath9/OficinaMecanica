resource "newrelic_one_dashboard" "oficina_mecanica" {
  name = "Oficina Mecanica - Fase 3"

  page {
    name = "Ordens de Servico e API"

    widget_bar {
      title  = "Volume diario de Ordens de Servico criadas"
      row    = 1
      column = 1
      width  = 6
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT count(*) FROM Metric WHERE metricName = 'ordens_servico.criadas' FACET dateOf(timestamp) SINCE 30 days ago"
      }
    }

    widget_line {
      title  = "Tempo medio de execucao por status (Diagnostico, Execucao, Finalizacao)"
      row    = 1
      column = 7
      width  = 6
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT average(ordens_servico.tempo_por_status_segundos) / 3600 AS 'Horas' FROM Metric FACET status TIMESERIES SINCE 30 days ago"
      }
    }

    widget_bar {
      title  = "Erros e falhas nas integracoes (ex.: envio de e-mail)"
      row    = 4
      column = 1
      width  = 6
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT count(*) FROM Metric WHERE metricName = 'integracoes.erros' FACET integracao SINCE 30 days ago"
      }
    }

    widget_line {
      title  = "Latencia das APIs (p50/p95, por rota)"
      row    = 4
      column = 7
      width  = 6
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT percentile(http.server.request.duration, 50, 95) FROM Metric WHERE metricName = 'http.server.request.duration' FACET http.route TIMESERIES SINCE 7 days ago"
      }
    }
  }
}
