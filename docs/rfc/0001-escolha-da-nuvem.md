# RFC 0001 — Escolha da nuvem

- **Status**: Aceito
- **Autor**: Taynan Konrath
- **Data**: 2026-08-30

## Contexto

A Fase 3 exige provisionar API Gateway, Function Serverless, banco de dados gerenciado e
cluster Kubernetes com IaC (Terraform), com deploy automatizado via CI/CD. A conta disponível
para o projeto é uma **AWS Academy Learner Lab**: sandbox gratuito, sem cartão de crédito, mas
com restrições importantes:

- Sem criação de usuários/roles IAM — só o `LabRole`/`LabInstanceProfile` pré-existentes podem
  ser anexados a recursos.
- Credenciais temporárias (Access Key + Secret + **Session Token**), que expiram em poucas horas
  e são revogadas sempre que a sessão do Lab é reiniciada — não há uma AWS Organization real por
  trás, é um ambiente didático provisionado sob demanda.
- Serviços com custo hora fixo mesmo ocioso (ex.: NAT Gateway, EKS control plane) consomem
  rapidamente o crédito limitado do Lab.

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **AWS (escolhida)** | Já disponível via Academy Lab; API Gateway + Lambda + RDS + EKS/EC2 cobrem os 4 requisitos de infra sem sair do mesmo provedor | Sem IAM próprio; credenciais expiram e precisam ser rotacionadas manualmente a cada sessão do Lab |
| Azure / GCP | Também têm free tier | Exigiria criar conta nova sem crédito educacional já disponível, e nenhuma vantagem técnica compensa isso pro escopo do desafio |
| Multi-cloud | Nenhuma | Complexidade de rede (VPN/peering entre provedores) sem nenhum requisito do desafio que justifique |

## Decisão

Usar **AWS**, via AWS Academy Learner Lab, para os quatro pilares de infraestrutura:

- **API Gateway**: AWS API Gateway (HTTP API) na frente da Function Serverless.
- **Function Serverless**: AWS Lambda (.NET 8) — repositório [oficina-mecanica-auth](https://github.com/konrath9/oficina-mecanica-auth).
- **Banco de Dados Gerenciado**: RDS PostgreSQL — repositório [oficina-mecanica-infra-db](https://github.com/konrath9/oficina-mecanica-infra-db) (justificativa detalhada na [RFC 0002](0002-escolha-do-banco-de-dados.md)).
- **Cluster Kubernetes**: k3s sobre EC2 — repositório [oficina-mecanica-infra-k8s](https://github.com/konrath9/oficina-mecanica-infra-k8s) (justificativa detalhada no [ADR 0001](../adr/0001-k3s-em-vez-de-eks.md)).

Cada um dos 4 repositórios provisiona sua fatia via Terraform com state remoto compartilhado em
S3 (bucket `oficina-mecanica-tfstate-159157616728`), permitindo repositórios/pipelines
independentes sem duplicar infraestrutura de rede (VPC compartilhada — ver [ADR 0002](../adr/0002-vpc-compartilhada-via-data-source.md)).

## Consequências

- **Positivo**: zero custo direto para o projeto; todos os serviços gerenciados exigidos pelo
  desafio (API Gateway, Lambda, RDS, cluster k8s) existem nativamente na AWS.
- **Negativo**: a rotatividade das credenciais do Learner Lab é uma fricção operacional real —
  os workflows de CI/CD dependem de secrets (`AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`/
  `AWS_SESSION_TOKEN`) que precisam ser atualizados manualmente sempre que a sessão do Lab expira
  ou é reiniciada. Isso é documentado nos READMEs dos repositórios de infraestrutura como uma
  limitação aceita do ambiente, não um defeito de arquitetura.
- **Negativo**: sem EKS gerenciado (ver ADR 0001) e sem NAT Gateway (subnets privadas do RDS não
  têm saída própria à internet — aceitável porque o RDS não precisa de egress).
