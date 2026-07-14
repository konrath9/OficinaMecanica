variable "create_kind_cluster" {
  description = "Se true, provisiona um cluster kind local. Se false, usa o kubeconfig apontado por external_kubeconfig_path (ex: um cluster cloud ja existente)."
  type        = bool
  default     = true
}

variable "external_kubeconfig_path" {
  description = "Caminho do kubeconfig a usar quando create_kind_cluster = false."
  type        = string
  default     = "~/.kube/config"
}

variable "cluster_name" {
  description = "Nome do cluster kind a ser criado."
  type        = string
  default     = "oficina-mecanica"
}

variable "kubernetes_namespace" {
  description = "Namespace onde a aplicacao e o banco de dados serao provisionados."
  type        = string
  default     = "oficina-mecanica"
}

variable "postgres_user" {
  description = "Usuario do PostgreSQL."
  type        = string
  default     = "postgres"
}

variable "postgres_password" {
  description = "Senha do PostgreSQL."
  type        = string
  default     = "postgres"
  sensitive   = true
}

variable "postgres_db" {
  description = "Nome do banco de dados."
  type        = string
  default     = "oficina_mecanica"
}

variable "postgres_storage_size" {
  description = "Tamanho do volume persistente do PostgreSQL."
  type        = string
  default     = "1Gi"
}
