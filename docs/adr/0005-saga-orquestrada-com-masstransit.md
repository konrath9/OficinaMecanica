# ADR 0005 — Saga orquestrada (MassTransit + RabbitMQ) no OS Service

- **Status:** aceita (Fase 4)
- **Substitui parcialmente:** [ADR 0003 — comunicação síncrona via REST](0003-comunicacao-sincrona-via-rest.md), que continua valendo para chamadas de cliente → API. Entre serviços, a comunicação passa a ser assíncrona.

## Contexto

A Fase 4 divide o monolito em três microsserviços (OS, Billing e Execution), cada um com banco próprio. A transação
"abrir OS → orçar → aprovar → pagar → executar → finalizar" deixa de caber numa transação ACID: cada etapa grava
num banco diferente. Uma falha no meio, como pagamento recusado ou peça indisponível durante o reparo, precisa desfazer o que
já aconteceu nos outros serviços (cancelar o orçamento, estornar o pagamento).

## Decisão

**Saga orquestrada**, com o orquestrador dentro do **OS Service** e implementada como `MassTransitStateMachine`
(`OficinaMecanica.Infrastructure/Mensageria/OrdemServicoStateMachine.cs`):

- O estado da saga é persistido no Postgres do OS Service (tabela `saga_ordem_servico`), no **mesmo DbContext** da OS,
  com lock pessimista por OS.
- O **outbox transacional** do MassTransit (`outbox_message`/`outbox_state`/`inbox_state`) grava a mudança da OS, o
  estado da saga e as mensagens a publicar num único `SaveChanges`. Sem ele haveria o problema clássico de dual-write
  (salvar no banco e cair antes de publicar, ou o contrário). O inbox deduplica as mensagens recebidas.
- O transporte é o RabbitMQ. Os participantes (Billing, Execution) só conhecem os contratos de mensagem
  ([docs/fase4/contratos-mensageria.md](../fase4/contratos-mensageria.md)), não conhecem uns aos outros.
- Compensações:

| Falha | Compensação comandada pelo orquestrador |
|---|---|
| Billing não gera o orçamento (`OrcamentoFalhou`) | OS permanece em diagnóstico; a falha fica registrada no histórico e o técnico pode reenviar |
| Cliente reprova / staff cancela antes do pagamento | `CancelarOrcamento` no Billing |
| Pagamento recusado (`PagamentoRecusado`) | `CancelarOrcamento` + OS cancelada |
| Falha na execução (`ExecucaoFalhou`) | `EstornarPagamento` (refund no Mercado Pago) → OS cancelada ao receber `PagamentoEstornado` |
| Staff cancela a OS em execução | `CancelarExecucao` + `EstornarPagamento` |

## Por que orquestração e não coreografia

- **O fluxo da OS é sequencial e tem dono claro.** É uma única linha do tempo, sem ramos paralelos, e o OS Service já era
  o dono do status. Com coreografia, essa sequência ficaria implícita, espalhada em handlers de três repositórios.
- **A compensação fica num lugar só.** "Se a execução falhar, estorne e depois cancele" é uma regra lida numa tela
  (`During(EmExecucao, When(ExecucaoFalhou)...)`). Com coreografia, cada serviço precisaria saber quem compensar.
- **Observabilidade.** O estado da saga pode ser consultado (`GET /api/ordens-servico/{id}/saga`) e o trace distribuído
  (`AddSource("MassTransit")` no OpenTelemetry → New Relic) mostra a cadeia OS → RabbitMQ → Billing/Execution.
- **Participantes mais simples.** Billing e Execution só reagem a comandos e publicam o resultado; não conhecem o fluxo.

**Custo aceito:** o OS Service vira ponto central do fluxo, e um deploy dele afeta a saga. O impacto é mitigado porque
as mensagens ficam enfileiradas no RabbitMQ enquanto o orquestrador está fora e a saga retoma de onde parou.

## Por que MassTransit

State machine declarativa com persistência em EF Core, outbox/inbox transacional, retry e propagação de trace
prontos. Escrever isso à mão sobre `RabbitMQ.Client` seria mais código e mais bug. Fixamos a **versão 8.x** (Apache 2.0),
porque a 9 é comercial. Usamos a 8.4.1, a última compatível com EF Core 8.
