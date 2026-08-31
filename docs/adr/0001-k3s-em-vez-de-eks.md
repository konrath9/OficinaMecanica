# ADR 0001 — k3s em EC2 em vez de EKS

- **Status**: Aceito
- **Repositório afetado**: [oficina-mecanica-infra-k8s](https://github.com/konrath9/oficina-mecanica-infra-k8s)
- **Data**: 2026-08-29

## Contexto

O desafio pede um "Cluster Kubernetes com escalabilidade" provisionado via Terraform. A opção
óbvia na AWS é o EKS (Elastic Kubernetes Service).

## Decisão

Usar **k3s** (distribuição leve de Kubernetes, CNCF-certificada) rodando numa única instância
EC2 `t3.small`, em vez de EKS.

## Motivo

O EKS cobra uma taxa fixa por hora do control plane (~US$0,10/h, além dos nodes) **mesmo
ocioso**, 24/7. Numa AWS Academy Learner Lab isso não é uma questão de "mais caro" — é uma questão
de **orçamento de crédito fixo e finito** por turma/aluno, que precisa durar o semestre inteiro
para várias entregas, não só esta fase. k3s roda de graça na mesma EC2 que já paga por si (o node
"worker"), sem control plane gerenciado cobrado à parte, e ainda vem com Traefik (ingress) e
ServiceLB embutidos, dispensando um Load Balancer gerenciado adicional (outro custo recorrente
do EKS via Service `type: LoadBalancer`).

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **k3s em EC2 (escolhida)** | Sem custo de control plane; Traefik e ServiceLB inclusos; single-binary, fácil de provisionar via `user_data` | Single-node (sem alta disponibilidade real de control plane); escalabilidade horizontal é de pods (HPA — ver ADR 0004), não de nodes |
| EKS | Gerenciado pela AWS, HA de control plane nativa, integra nativamente com IAM/ALB | Custo fixo por hora não compatível com o crédito limitado do Learner Lab; setup (VPC CNI, node groups, add-ons) desproporcional ao escopo do desafio |
| kind/minikube num único host | Zero custo | Não é um cluster "real" acessível de fora — serviria só para CI (e é exatamente o que o repositório principal já usa para validar os manifestos em `kind`, efêmero, dentro do próprio runner) |

## Consequências

- **Positivo**: custo zero de control plane, permitindo usar o mesmo crédito do Learner Lab nos
  outros três pilares de infra (Lambda, API Gateway, RDS) sem risco de estourar o orçamento no
  meio do semestre.
- **Negativo**: sem alta disponibilidade de control plane — se a EC2 cair, o cluster inteiro cai
  junto (mitigado parcialmente por ser um ambiente de estudo, não produção real).
- **Negativo**: escalabilidade limitada ao tamanho de uma única instância (HPA redistribui pods
  entre CPU/memória disponíveis no node, mas não cria novos nodes) — documentado como trade-off
  aceito, não como atendimento pleno de "alta disponibilidade" em sentido de produção.
