# Infraestrutura como Código (Terraform)

Provisiona, via Terraform, tudo o que a aplicação precisa **antes** de aplicar os manifestos Kubernetes de `/k8s`: o cluster e o banco de dados.

## Recursos criados

| Recurso | Tipo Terraform | Descrição |
|---|---|---|
| Cluster Kubernetes local | `kind_cluster.oficina` | Cluster [kind](https://kind.sigs.k8s.io/) criado do zero via Docker do host (pode ser desabilitado com `create_kind_cluster = false` para usar um cluster já existente, local ou cloud). |
| Namespace | `kubernetes_namespace.oficina` | Namespace `oficina-mecanica`, onde a aplicação (`/k8s`) e o banco de dados são provisionados. |
| Credenciais do Postgres | `kubernetes_secret.postgres_credentials` | `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`. |
| Volume do Postgres | `kubernetes_persistent_volume_claim.postgres_data` | Armazenamento persistente (`1Gi` por padrão) para os dados do banco. |
| Banco de dados | `kubernetes_stateful_set_v1.postgres` + `kubernetes_service_v1.postgres` | PostgreSQL 16 (mesma imagem do `docker-compose.yml`), exposto internamente como `postgres:5432` — nome usado pela API em `k8s/secret.yaml`. |
| Autoscaling | `helm_release.metrics_server` | Instala o `metrics-server` no cluster — pré-requisito técnico para o HPA (`k8s/hpa.yaml`) conseguir ler `%CPU`/memória dos pods. |

## Pré-requisitos

- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.5
- [Docker](https://www.docker.com/) em execução (usado pelo provider `kind` para criar o cluster)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)

## Como aplicar

```bash
cd infra
terraform init
terraform apply -auto-approve
```

Isso cria o cluster kind local, o namespace, o Postgres e o metrics-server.

Para usar o cluster provisionado com `kubectl`:

```bash
export KUBECONFIG=$(terraform output -raw kubeconfig_path)
kubectl get pods -n oficina-mecanica
```

Em seguida, aplique os manifestos da aplicação (ver `/k8s`):

```bash
kubectl apply -f ../k8s
kubectl rollout status deployment/oficina-mecanica-api -n oficina-mecanica
```

## Como desprovisionar

```bash
terraform destroy -auto-approve
```

## Usando um cluster já existente (cloud ou local)

Copie `terraform.tfvars.example` para `terraform.tfvars` e defina:

```hcl
create_kind_cluster      = false
external_kubeconfig_path = "~/.kube/config"
```

Nesse caso o Terraform não cria um cluster kind — ele só provisiona namespace, Postgres e metrics-server no cluster já apontado pelo seu kubeconfig atual (ex: AKS, EKS, GKE ou outro cluster local).
