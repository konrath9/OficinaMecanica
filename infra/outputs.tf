output "kubeconfig_path" {
  description = "Caminho do kubeconfig a usar com kubectl para acessar o cluster provisionado."
  value       = local.kubeconfig_path
}

output "cluster_name" {
  description = "Nome do cluster kind provisionado."
  value       = var.cluster_name
}

output "kubernetes_namespace" {
  description = "Namespace onde a aplicacao e o banco de dados foram provisionados."
  value       = kubernetes_namespace.oficina.metadata[0].name
}

output "postgres_service_fqdn" {
  description = "Nome do Service do PostgreSQL dentro do cluster (usado na connection string da API)."
  value       = "${kubernetes_service_v1.postgres.metadata[0].name}.${kubernetes_namespace.oficina.metadata[0].name}.svc.cluster.local"
}
