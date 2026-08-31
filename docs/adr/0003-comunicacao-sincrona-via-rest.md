# ADR 0003 — Comunicação síncrona via REST/HTTP entre os 4 repositórios

- **Status**: Aceito
- **Repositórios afetados**: todos os 4
- **Data**: 2026-08-30

## Contexto

Com a segregação em 4 repositórios (Lambda, infra k8s, infra DB, aplicação principal), existem
dois pontos de integração real em tempo de execução: (1) o cliente autentica na Function
Serverless e depois chama a API principal com o token recebido; (2) a API principal lê/escreve no
RDS. Era preciso decidir o padrão de comunicação entre o cliente final e os dois serviços de
borda (Lambda e API).

## Decisão

Comunicação **síncrona via REST/HTTP**, sem mensageria assíncrona (fila, pub/sub) entre os
componentes:

- Cliente → API Gateway → Lambda (`POST /auth/login`): request/response HTTP síncrono, o cliente
  espera o JWT na mesma chamada.
- Cliente → Traefik (API Gateway do k8s) → API principal: todas as rotas são REST síncronas
  (`GET`/`POST`/`PUT`/`PATCH`/`DELETE`), incluindo o acompanhamento da OS.
- API principal → RDS: acesso síncrono via Npgsql/EF Core, sem CQRS nem event sourcing.

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **REST síncrono (escolhida)** | Simples de implementar, testar (WebApplicationFactory) e depurar; latência previsível; nenhum requisito do desafio pede desacoplamento assíncrono entre login e consumo da API | Login e consulta de OS ficam acoplados no tempo — se a Lambda estiver lenta/fora do ar, o cliente não consegue autenticar |
| Fila (SQS) entre Lambda e API para eventos de autenticação | Desacopla os serviços no tempo | Não existe um "evento" de autenticação que precise ser processado depois — o cliente precisa do JWT *imediatamente* pra fazer a próxima chamada; assíncrono aqui só adicionaria latência e complexidade sem ganho |
| Notificações por e-mail via fila (em vez de SMTP direto) | Resiliente a falhas do serviço de e-mail | Fora do escopo desta fase — a notificação por e-mail já existe desde fases anteriores via SMTP síncrono (Mailpit local); não foi um requisito da Fase 3 revisitar esse ponto |

## Consequências

- **Positivo**: toda a superfície de integração é HTTP/JSON documentado via Swagger, testável com
  Postman/curl sem infraestrutura extra (sem broker de mensagens pra subir em CI).
- **Negativo**: acoplamento temporal — uma Lambda fria (cold start) ou um RDS lento atrasam
  diretamente a resposta ao cliente, sem um buffer assíncrono no meio. Aceitável para o volume de
  uma oficina (baixo throughput), não necessariamente para um cenário de alta escala.
