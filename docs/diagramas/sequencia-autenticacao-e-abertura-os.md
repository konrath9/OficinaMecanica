# Diagrama de Sequência — autenticação via CPF e abertura de Ordem de Serviço

Cobre o fluxo pedido pelo desafio: autenticação do cliente por CPF (Function Serverless) e
abertura de uma Ordem de Serviço, incluindo a etapa de acompanhamento/aprovação pelo próprio
cliente autenticado.

```mermaid
sequenceDiagram
    actor Cliente
    actor Recepcionista as Recepcionista (staff)
    participant GW as API Gateway (AWS)
    participant Lambda as Lambda (oficina-mecanica-auth)
    participant RDS as RDS PostgreSQL
    participant Traefik as Traefik (API Gateway k8s)
    participant API as API principal (OficinaMecanica)

    Note over Cliente,RDS: 1) Autenticacao via CPF
    Cliente->>GW: POST /auth/login { cpf }
    GW->>Lambda: invoca function
    Lambda->>Lambda: valida digito verificador do CPF
    alt CPF invalido
        Lambda-->>GW: 401 Unauthorized
        GW-->>Cliente: 401
    else CPF valido
        Lambda->>RDS: SELECT id, ativo FROM clientes WHERE documento = :cpf
        alt cliente nao existe ou ativo = false
            Lambda-->>GW: 401 Unauthorized
            GW-->>Cliente: 401
        else cliente ativo
            Lambda->>Lambda: gera JWT (sub=clienteId, role=Cliente, exp=1h)
            Lambda-->>GW: 200 OK { token }
            GW-->>Cliente: 200 OK { token }
        end
    end

    Note over Recepcionista,API: 2) Staff abre a Ordem de Servico
    Recepcionista->>Traefik: POST /api/ordens-servico { clienteId, veiculoId }\nBearer <token staff>
    Traefik->>API: encaminha requisicao
    API->>API: valida JWT (role staff) + [Authorize(Roles=...)]
    API->>RDS: valida cliente/veiculo, cria OS (status=Recebida)
    RDS-->>API: OS criada
    API-->>Traefik: 201 Created { id, numero }
    Traefik-->>Recepcionista: 201 Created

    Note over Recepcionista,API: 3) Staff adiciona servicos/pecas e conclui o diagnostico
    Recepcionista->>API: POST /api/ordens-servico/{id}/servicos, /pecas
    API->>RDS: grava itens (snapshot preco/descricao)
    Recepcionista->>API: POST /api/ordens-servico/{id}/concluir-diagnostico
    API->>RDS: status = AguardandoAprovacao

    Note over Cliente,API: 4) Cliente acompanha e aprova o orcamento (mesmo token do passo 1)
    Cliente->>Traefik: GET /api/acompanhamento/{numero}\nBearer <token Cliente>
    Traefik->>API: encaminha requisicao
    API->>API: valida JWT (role Cliente) + checa posse (sub == ClienteId da OS)
    alt token nao pertence ao dono da OS
        API-->>Cliente: 403 Forbidden
    else token do proprio cliente
        API->>RDS: consulta status/orcamento da OS
        API-->>Cliente: 200 OK { status, orcamento, ... }
        Cliente->>Traefik: POST /api/acompanhamento/{numero}/aprovar\nBearer <token Cliente>
        Traefik->>API: encaminha requisicao
        API->>RDS: status = EmExecucao
        API-->>Cliente: 200 OK
    end
```

## Pontos de decisão relevantes

- O **mesmo JWT** obtido no passo 1 é reutilizado nos passos 4 (acompanhamento/aprovação) — não
  existe um segundo login para o cliente acompanhar a própria OS (ver [RFC 0003](../rfc/0003-estrategia-de-autenticacao.md)).
- A checagem de posse (`sub == ClienteId`) acontece **na API principal**, não no API Gateway —
  o Traefik só roteia; quem decide autorização é a aplicação (ver [ADR 0003](../adr/0003-comunicacao-sincrona-via-rest.md)).
- Um cliente inativo (`ativo = false`) nunca recebe token no passo 1 — a Function Serverless
  nega antes mesmo do JWT ser gerado.
