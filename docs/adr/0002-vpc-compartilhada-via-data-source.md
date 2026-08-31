# ADR 0002 — VPC compartilhada entre repositórios via data source, não remote state

- **Status**: Aceito
- **Repositórios afetados**: [oficina-mecanica-infra-db](https://github.com/konrath9/oficina-mecanica-infra-db), [oficina-mecanica-infra-k8s](https://github.com/konrath9/oficina-mecanica-infra-k8s)
- **Data**: 2026-08-29

## Contexto

O desafio pede repositórios de infraestrutura **separados** para o banco (`infra-db`) e o cluster
Kubernetes (`infra-k8s`), mas ambos precisam viver na mesma VPC (o cluster k3s precisa alcançar o
RDS na porta 5432, e ambos compartilham a mesma subnet pública/privadas). Isso levanta a questão
de como um repositório Terraform referencia recursos criados por outro, sem os dois virarem um
monólito.

## Decisão

`infra-db` cria a VPC, subnets e tags; `infra-k8s` **não** referencia o `state` remoto de
`infra-db` diretamente. Em vez disso, usa `data source` do provider AWS filtrando por **tag**
(`Project = "oficina-mecanica"`) para localizar a VPC e a subnet pública já existentes:

```hcl
data "aws_vpc" "shared" {
  filter {
    name   = "tag:Project"
    values = [var.project_name]
  }
}
```

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **Data source por tag (escolhida)** | Repositórios genuinamente independentes — `infra-k8s` não precisa de permissão de leitura no bucket S3 do state do `infra-db`, nem conhece a estrutura interna dos outputs dele; só depende de uma convenção de tag | Se a tag mudar ou a VPC for recriada com nome diferente, a busca falha silenciosamente até o `apply` (mitigado por `terraform plan` rodar em toda PR) |
| `terraform_remote_state` (ler o state do outro repo via S3) | Outputs tipados, falha cedo se o output não existir | Acopla os dois repositórios ao **formato interno do state** um do outro — mudar um nome de recurso em `infra-db` quebra `infra-k8s` sem aviso explícito, exatamente o tipo de acoplamento que "repositórios separados" deveria evitar |
| Um único repositório Terraform para VPC + DB + k8s | Sem duplicação de lookup | Contraria diretamente o requisito do desafio de repositórios segregados com CI/CD próprio cada um |

## Consequências

- **Positivo**: `infra-k8s` pode rodar seu `terraform apply` mesmo que `infra-db` nunca tenha
  sido aplicado ainda no mesmo dia de trabalho (falha de forma clara — "VPC não encontrada" — em
  vez de um erro de acesso a state alheio).
- **Positivo**: nenhum dos dois repositórios precisa de permissão de leitura no bucket S3 de
  state do outro; cada CI/CD só lê/escreve o próprio arquivo de state (`db/terraform.tfstate` e
  `k8s/terraform.tfstate` no mesmo bucket compartilhado, mas caminhos distintos).
- **Negativo**: não há validação de schema entre os dois — um erro de digitação na tag
  `Project` quebra a integração de forma só detectável em `plan`/`apply`, não em tempo de escrita
  do HCL.
