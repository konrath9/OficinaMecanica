# Documentação da Arquitetura — Fase 3

Índice da documentação exigida pelo Tech Challenge Fase 3 (SOAT/FIAP).

## Diagramas

- [Diagrama de Componentes](diagramas/componentes.md) — visão de nuvem, APIs, banco e monitoramento
- [Diagrama de Sequência](diagramas/sequencia-autenticacao-e-abertura-os.md) — autenticação via CPF e abertura de OS

## Modelo de dados

- [Modelo de dados](modelo-dados.md) — diagrama ER, justificativa da escolha do banco e dos relacionamentos

## RFCs (decisões técnicas)

- [0001 — Escolha da nuvem](rfc/0001-escolha-da-nuvem.md)
- [0002 — Escolha do banco de dados](rfc/0002-escolha-do-banco-de-dados.md)
- [0003 — Estratégia de autenticação](rfc/0003-estrategia-de-autenticacao.md)

## ADRs (decisões arquiteturais permanentes)

- [0001 — k3s em vez de EKS](adr/0001-k3s-em-vez-de-eks.md)
- [0002 — VPC compartilhada via data source](adr/0002-vpc-compartilhada-via-data-source.md)
- [0003 — Comunicação síncrona via REST](adr/0003-comunicacao-sincrona-via-rest.md)
- [0004 — Uso de HPA para escalabilidade](adr/0004-uso-de-hpa-para-escalabilidade.md)

## Repositórios do projeto

| Repositório | Responsabilidade |
|---|---|
| [OficinaMecanica](https://github.com/konrath9/OficinaMecanica) | Aplicação principal (executa em Kubernetes) |
| [oficina-mecanica-auth](https://github.com/konrath9/oficina-mecanica-auth) | Function Serverless de autenticação via CPF |
| [oficina-mecanica-infra-k8s](https://github.com/konrath9/oficina-mecanica-infra-k8s) | Infraestrutura Kubernetes (Terraform) |
| [oficina-mecanica-infra-db](https://github.com/konrath9/oficina-mecanica-infra-db) | Infraestrutura do banco de dados gerenciado (Terraform) |
