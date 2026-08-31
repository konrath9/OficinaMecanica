# ADR 0004 — HPA (Horizontal Pod Autoscaler) para escalabilidade da API

- **Status**: Aceito
- **Repositório afetado**: [OficinaMecanica](https://github.com/konrath9/OficinaMecanica) (`k8s/hpa.yaml`)
- **Data**: 2026-08-30 (revisado ao ligar o deploy real no k3s de produção)

## Contexto

O desafio exige "Cluster Kubernetes com escalabilidade". Como o cluster em si é um único node
k3s (ver [ADR 0001](0001-k3s-em-vez-de-eks.md)), a escalabilidade não pode vir de mais *nodes* —
precisa vir de melhor aproveitamento dos recursos do node existente, escalando a quantidade de
**réplicas do pod** conforme a demanda.

## Decisão

`k8s/hpa.yaml` usa `autoscaling/v2` HorizontalPodAutoscaler visando o `Deployment
oficina-mecanica-api`, com:

- **1 a 3 réplicas**.
- Alvo de **70% de CPU** e **80% de memória** (média entre os pods).
- `metrics-server` instalado via Terraform (`infra/metrics-server.tf`, cluster de CI) e via
  `user_data` do k3s (embutido na distribuição) em produção, necessário pro HPA calcular as
  métricas de uso.

Cada réplica pede `100m` CPU / `128Mi` memória e limita em `300m` CPU / `256Mi` memória
(`k8s/deployment.yaml`) — dimensionado para caber 3 réplicas simultâneas dentro dos ~2GB de RAM
do node `t3.small` sem competir com Traefik, CoreDNS e o próprio k3s.

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **HPA por CPU/memória (escolhida)** | Nativo do Kubernetes, sem dependência externa; reage a carga real da aplicação | Não escala além da capacidade de um único node (sem Cluster Autoscaler, porque não há node group — ver ADR 0001) |
| KEDA (autoscaling orientado a eventos) | Escalaria por métricas de negócio (ex. fila de OS pendentes) | Nenhuma fila/evento existe na arquitetura atual (ver ADR 0003 — tudo é síncrono); adicionaria um operator extra sem métrica pra consumir |
| Réplica fixa (sem HPA) | Mais simples | Não atende ao requisito explícito de "cluster com escalabilidade"; desperdiça recursos em baixa carga ou satura em pico, sem meio-termo |

## Consequências

- **Positivo**: a API absorve picos de tráfego (ex. vídeo de demonstração fazendo várias chamadas
  seguidas, ou múltiplos usuários testando ao mesmo tempo) sem intervenção manual, dentro do teto
  de 3 réplicas.
- **Negativo**: o teto de 3 réplicas é uma escolha de capacidade do node, não uma escala elástica
  de verdade — em um cenário de crescimento real (múltiplas unidades da oficina, como o enunciado
  do desafio descreve), o próximo passo natural seria migrar para um node group com Cluster
  Autoscaler (o que reabriria a discussão de custo do [ADR 0001](0001-k3s-em-vez-de-eks.md)).
