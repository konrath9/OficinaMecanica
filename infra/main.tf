# Cluster Kubernetes local (kind). Recurso real, criado do zero via Docker do host.
resource "kind_cluster" "oficina" {
  count           = var.create_kind_cluster ? 1 : 0
  name            = var.cluster_name
  wait_for_ready  = true
}

locals {
  kubeconfig_path = var.create_kind_cluster ? one(kind_cluster.oficina[*].kubeconfig_path) : var.external_kubeconfig_path
}

resource "kubernetes_namespace" "oficina" {
  metadata {
    name = var.kubernetes_namespace
  }

  depends_on = [kind_cluster.oficina]
}

resource "kubernetes_secret" "postgres_credentials" {
  metadata {
    name      = "postgres-credentials"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  data = {
    POSTGRES_USER     = var.postgres_user
    POSTGRES_PASSWORD = var.postgres_password
    POSTGRES_DB       = var.postgres_db
  }
}

resource "kubernetes_persistent_volume_claim" "postgres_data" {
  metadata {
    name      = "postgres-data"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  spec {
    access_modes = ["ReadWriteOnce"]
    resources {
      requests = {
        storage = var.postgres_storage_size
      }
    }
  }

  wait_until_bound = false
}

resource "kubernetes_stateful_set_v1" "postgres" {
  metadata {
    name      = "postgres"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  spec {
    service_name = "postgres"
    replicas     = 1

    selector {
      match_labels = {
        app = "postgres"
      }
    }

    template {
      metadata {
        labels = {
          app = "postgres"
        }
      }

      spec {
        container {
          name  = "postgres"
          image = "postgres:16-alpine"

          port {
            container_port = 5432
          }

          env_from {
            secret_ref {
              name = kubernetes_secret.postgres_credentials.metadata[0].name
            }
          }

          volume_mount {
            name       = "postgres-data"
            mount_path = "/var/lib/postgresql/data"
          }

          readiness_probe {
            exec {
              command = ["pg_isready", "-U", var.postgres_user]
            }
            initial_delay_seconds = 5
            period_seconds         = 10
          }

          liveness_probe {
            exec {
              command = ["pg_isready", "-U", var.postgres_user]
            }
            initial_delay_seconds = 10
            period_seconds         = 15
          }
        }

        volume {
          name = "postgres-data"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim.postgres_data.metadata[0].name
          }
        }
      }
    }
  }
}

resource "kubernetes_service_v1" "postgres" {
  metadata {
    name      = "postgres"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  spec {
    selector = {
      app = "postgres"
    }

    port {
      port        = 5432
      target_port = 5432
    }

    type = "ClusterIP"
  }
}
