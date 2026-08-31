# Diagrama de Componentes — visão de nuvem, APIs, banco e monitoramento

Visão completa dos 4 repositórios e como eles se conectam em produção (AWS, via AWS Academy
Learner Lab). Cada bloco pontilhado é um repositório Git com CI/CD próprio.

```mermaid
flowchart TB
    Cliente["Cliente final\n(app/Postman)"]
    Staff["Staff da oficina\n(Administrador/Mecanico/Recepcionista)"]

    subgraph AuthRepo["oficina-mecanica-auth (Lambda)"]
        APIGW["AWS API Gateway\n(HTTP API)"]
        Lambda["Lambda .NET 8\nPOST /auth/login\nvalida CPF, consulta cliente, emite JWT"]
        APIGW --> Lambda
    end

    subgraph K8sRepo["oficina-mecanica-infra-k8s (Terraform)"]
        subgraph EC2["EC2 t3.small - k3s"]
            Traefik["Traefik\n(API Gateway / Ingress)"]
            subgraph NS["namespace oficina-mecanica"]
                API["Deployment oficina-mecanica-api\n1-3 replicas (HPA CPU/mem)"]
                Mailpit["Deployment mailpit\n(SMTP + UI de demo)"]
            end
            CoreDNS["CoreDNS"]
            MetricsServer["metrics-server"]
        end
    end

    subgraph DBRepo["oficina-mecanica-infra-db (Terraform)"]
        RDS[("RDS PostgreSQL 16\nsubnets privadas, sem IP publico")]
    end

    subgraph AppRepo["OficinaMecanica (aplicacao principal)"]
        direction TB
        Logs["Serilog -> stdout JSON\n(correlation id por request)"]
        Health["/health\n(readiness/liveness probe)"]
    end

    Cliente -->|"1: POST /auth/login {cpf}"| APIGW
    Lambda -->|"2: SELECT id, ativo FROM clientes"| RDS
    Lambda -->|"3: 200 OK {token JWT, role=Cliente}"| Cliente

    Cliente -->|"4: chamadas autenticadas\nBearer <token>"| Traefik
    Staff -->|"login e/senha + chamadas administrativas"| Traefik
    Traefik --> API
    API -->|"valida assinatura/expiracao do JWT\n(mesmo issuer/audience/secret da Lambda)"| API
    API --> RDS
    API -->|"notificacao por status"| Mailpit
    API -.->|logs estruturados| Logs
    API -.->|healthcheck de banco| Health
    MetricsServer -.->|CPU/memoria| API

    style AuthRepo stroke-dasharray: 4 4
    style K8sRepo stroke-dasharray: 4 4
    style DBRepo stroke-dasharray: 4 4
    style AppRepo stroke-dasharray: 4 4
```

## Legenda por repositório

| Repositório | Responsabilidade | Componentes neste diagrama |
|---|---|---|
| [oficina-mecanica-auth](https://github.com/konrath9/oficina-mecanica-auth) | Function Serverless de autenticação via CPF | AWS API Gateway, Lambda |
| [oficina-mecanica-infra-k8s](https://github.com/konrath9/oficina-mecanica-infra-k8s) | Cluster Kubernetes | EC2 (k3s), Traefik, CoreDNS, metrics-server |
| [oficina-mecanica-infra-db](https://github.com/konrath9/oficina-mecanica-infra-db) | Banco de dados gerenciado | RDS PostgreSQL |
| [OficinaMecanica](https://github.com/konrath9/OficinaMecanica) | Aplicação principal + manifestos K8s | Deployment/Service/HPA/Ingress da API e do Mailpit, logs, health check |

## Observabilidade

- **Logs estruturados** (JSON, via Serilog) na saída padrão de cada pod, com `X-Correlation-Id`
  gerado/ecoado pelo `CorrelationIdMiddleware` — permite correlacionar todas as linhas de log de
  uma mesma requisição.
- **`/health`** consultado pelas probes de readiness/liveness do Kubernetes (`k8s/deployment.yaml`)
  e é o endpoint que o pipeline de CI/CD usa como smoke test pós-deploy.
- **`metrics-server`** alimenta o HPA (CPU/memória) — ver [ADR 0004](../adr/0004-uso-de-hpa-para-escalabilidade.md).
