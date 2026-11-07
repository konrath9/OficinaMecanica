resource "newrelic_one_dashboard" "oficina_mecanica" {
  name = "Oficina Mecanica - Fase 4"

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
        # sum() do contador, nao count(*): count(*) contaria os pontos de exportacao
        # (um a cada 60s), nao as OS criadas.
        query = "SELECT sum(ordens_servico.criadas) FROM Metric FACET dateOf(timestamp) SINCE 30 days ago"
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
        query      = "SELECT sum(integracoes.erros) FROM Metric FACET integracao SINCE 30 days ago"
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
        # Atributo com ponto no nome precisa de crase no FACET, senao o NRQL nao resolve.
        query = "SELECT percentile(http.server.request.duration, 50, 95) FROM Metric FACET `http.route` TIMESERIES SINCE 7 days ago"
      }
    }
  }

  # Fase 4: fluxo distribuido entre OS, Billing e Execution (Saga orquestrada via RabbitMQ).
  # Spans de mensageria vem do ActivitySource "MassTransit" (AddSource no OpenTelemetry dos 3 servicos).
  page {
    name = "Saga e microsservicos (Fase 4)"

    widget_line {
      title  = "Mensagens da Saga publicadas/consumidas por servico"
      row    = 1
      column = 1
      width  = 6
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT count(*) FROM Span WHERE span.kind IN ('producer', 'consumer') FACET service.name, span.kind TIMESERIES SINCE 1 day ago"
      }
    }

    widget_table {
      title  = "Mensagens por tipo (comandos e eventos da Saga)"
      row    = 1
      column = 7
      width  = 6
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT count(*) AS 'Mensagens', average(duration.ms) AS 'Duracao media (ms)' FROM Span WHERE span.kind IN ('producer', 'consumer') FACET service.name, name SINCE 1 day ago LIMIT 40"
      }
    }

    widget_billboard {
      title  = "Pagamentos (Billing / Mercado Pago)"
      row    = 4
      column = 1
      width  = 4
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT sum(pagamentos.aprovados) AS 'Aprovados', sum(pagamentos.recusados) AS 'Recusados (compensacao)', sum(pagamentos.estornados) AS 'Estornados (compensacao)' FROM Metric SINCE 1 day ago"
      }
    }

    widget_billboard {
      title  = "Execucao (Execution Service)"
      row    = 4
      column = 5
      width  = 4
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT sum(execucao.falhas) AS 'Falhas (disparam estorno)', average(execucao.duracao_segundos) AS 'Duracao media (s)' FROM Metric SINCE 1 day ago"
      }
    }

    widget_bar {
      title  = "Erros por microsservico"
      row    = 4
      column = 9
      width  = 4
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT count(*) FROM Span WHERE otel.status_code = 'ERROR' FACET service.name SINCE 1 day ago"
      }
    }

    widget_line {
      title  = "Latencia p95 das APIs por microsservico"
      row    = 7
      column = 1
      width  = 12
      height = 3

      nrql_query {
        account_id = var.newrelic_account_id
        query      = "SELECT percentile(duration.ms, 95) FROM Span WHERE span.kind = 'server' FACET service.name TIMESERIES SINCE 1 day ago"
      }
    }
  }
}
