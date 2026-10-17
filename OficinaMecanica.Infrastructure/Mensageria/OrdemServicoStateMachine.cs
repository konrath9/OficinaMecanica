using MassTransit;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Contracts;

namespace OficinaMecanica.Infrastructure.Mensageria
{
    /// <summary>Estado persistido da Saga de uma OS (tabela saga_ordem_servico). CorrelationId = Id da OS.</summary>
    public class OrdemServicoSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = null!;
        public Guid? OrcamentoId { get; set; }
        public Guid? PagamentoId { get; set; }
        public decimal? Valor { get; set; }
        public string? UltimoMotivo { get; set; }
        public DateTime CriadaEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
    }

    /// <summary>
    /// Orquestrador da Saga da OS: OS -> orçamento (Billing) -> aprovação -> pagamento (Billing/Mercado Pago)
    /// -> execução (Execution) -> finalização, com compensações em cada etapa. O que muda na OS em cada passo
    /// fica em <see cref="SagaOrdemServicoAcoes"/>; aqui só a sequência e os comandos publicados.
    /// Fluxo documentado em docs/fase4/contratos-mensageria.md.
    /// </summary>
    public class OrdemServicoStateMachine : MassTransitStateMachine<OrdemServicoSagaState>
    {
        public State AguardandoOrcamento { get; private set; } = null!;
        public State FalhaOrcamento { get; private set; } = null!;
        public State AguardandoAprovacao { get; private set; } = null!;
        public State AguardandoPagamento { get; private set; } = null!;
        public State EmExecucao { get; private set; } = null!;
        public State Estornando { get; private set; } = null!;
        public State Finalizada { get; private set; } = null!;
        public State Cancelada { get; private set; } = null!;

        public Event<DiagnosticoConcluido> DiagnosticoConcluido { get; private set; } = null!;
        public Event<OrcamentoGerado> OrcamentoGerado { get; private set; } = null!;
        public Event<OrcamentoFalhou> OrcamentoFalhou { get; private set; } = null!;
        public Event<OrcamentoAprovadoPeloCliente> OrcamentoAprovadoPeloCliente { get; private set; } = null!;
        public Event<PagamentoCriado> PagamentoCriado { get; private set; } = null!;
        public Event<PagamentoAprovado> PagamentoAprovado { get; private set; } = null!;
        public Event<PagamentoRecusado> PagamentoRecusado { get; private set; } = null!;
        public Event<PagamentoEstornado> PagamentoEstornado { get; private set; } = null!;
        public Event<ExecucaoIniciada> ExecucaoIniciada { get; private set; } = null!;
        public Event<ExecucaoFinalizada> ExecucaoFinalizada { get; private set; } = null!;
        public Event<ExecucaoFalhou> ExecucaoFalhou { get; private set; } = null!;
        public Event<OrdemServicoCancelada> OrdemServicoCancelada { get; private set; } = null!;

        public OrdemServicoStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => DiagnosticoConcluido, e => e.CorrelateById(m => m.Message.OrdemServicoId));
            Correlacionar(() => OrcamentoGerado, m => m.OrdemServicoId);
            Correlacionar(() => OrcamentoFalhou, m => m.OrdemServicoId);
            Correlacionar(() => OrcamentoAprovadoPeloCliente, m => m.OrdemServicoId);
            Correlacionar(() => PagamentoCriado, m => m.OrdemServicoId);
            Correlacionar(() => PagamentoAprovado, m => m.OrdemServicoId);
            Correlacionar(() => PagamentoRecusado, m => m.OrdemServicoId);
            Correlacionar(() => PagamentoEstornado, m => m.OrdemServicoId);
            Correlacionar(() => ExecucaoIniciada, m => m.OrdemServicoId);
            Correlacionar(() => ExecucaoFinalizada, m => m.OrdemServicoId);
            Correlacionar(() => ExecucaoFalhou, m => m.OrdemServicoId);
            Correlacionar(() => OrdemServicoCancelada, m => m.OrdemServicoId);

            // Mensagem duplicada ou fora de ordem (at-least-once) num estado que não a espera: ignora em vez de falhar.
            OnUnhandledEvent(ctx => ctx.Ignore());

            BeforeEnterAny(x => x.Then(ctx => ctx.Saga.AtualizadaEm = DateTime.UtcNow));

            // 1. Diagnóstico concluído -> pede o orçamento ao Billing
            Initially(
                When(DiagnosticoConcluido)
                    .Then(ctx => ctx.Saga.CriadaEm = DateTime.UtcNow)
                    .ThenAsync(PublicarGerarOrcamento)
                    .TransitionTo(AguardandoOrcamento));

            // Billing falhou antes: o técnico pode concluir o diagnóstico de novo (ex.: após corrigir os itens)
            During(FalhaOrcamento,
                When(DiagnosticoConcluido)
                    .ThenAsync(PublicarGerarOrcamento)
                    .TransitionTo(AguardandoOrcamento));

            // 2. Orçamento gerado -> OS aguarda aprovação do cliente | falhou -> compensa (OS segue em diagnóstico)
            During(AguardandoOrcamento,
                When(OrcamentoGerado)
                    .Then(ctx =>
                    {
                        ctx.Saga.OrcamentoId = ctx.Message.OrcamentoId;
                        ctx.Saga.Valor = ctx.Message.Valor;
                    })
                    .ThenAsync(ctx => Acoes(ctx).EnviarParaAprovacaoAsync(ctx.Saga.CorrelationId, ctx.CancellationToken))
                    .TransitionTo(AguardandoAprovacao),
                When(OrcamentoFalhou)
                    .Then(ctx => ctx.Saga.UltimoMotivo = ctx.Message.Motivo)
                    .ThenAsync(ctx => Acoes(ctx).RegistrarFalhaOrcamentoAsync(ctx.Saga.CorrelationId, ctx.Message.Motivo, ctx.CancellationToken))
                    .TransitionTo(FalhaOrcamento));

            // 3. Cliente aprovou -> Billing cria a cobrança no Mercado Pago
            During(AguardandoAprovacao,
                When(OrcamentoAprovadoPeloCliente)
                    .Publish(ctx => new AprovarOrcamento(ctx.Saga.CorrelationId))
                    .TransitionTo(AguardandoPagamento));

            // 4. Pagamento aprovado -> envia para a fila de execução | recusado -> compensa (cancela orçamento e OS)
            During(AguardandoPagamento,
                When(PagamentoCriado)
                    .Then(ctx => ctx.Saga.PagamentoId = ctx.Message.PagamentoId)
                    .ThenAsync(ctx => Acoes(ctx).RegistrarLinkPagamentoAsync(ctx.Saga.CorrelationId, ctx.Message.LinkPagamento, ctx.CancellationToken)),
                When(PagamentoAprovado)
                    .Then(ctx => ctx.Saga.PagamentoId = ctx.Message.PagamentoId)
                    .ThenAsync(async ctx =>
                    {
                        var comando = await Acoes(ctx).ConfirmarPagamentoAsync(ctx.Saga.CorrelationId, ctx.CancellationToken);
                        await ctx.Publish(comando, ctx.CancellationToken);
                    })
                    .TransitionTo(EmExecucao),
                When(PagamentoRecusado)
                    .Then(ctx => ctx.Saga.UltimoMotivo = $"Pagamento recusado: {ctx.Message.Motivo}")
                    .ThenAsync(ctx => Acoes(ctx).CancelarAsync(ctx.Saga.CorrelationId, ctx.Saga.UltimoMotivo!, ctx.CancellationToken))
                    .Publish(ctx => new CancelarOrcamento(ctx.Saga.CorrelationId, ctx.Saga.UltimoMotivo!))
                    .TransitionTo(Cancelada));

            // OS cancelada (staff ou cliente reprovou) antes da execução -> cancela o orçamento no Billing
            During(AguardandoOrcamento, AguardandoAprovacao, AguardandoPagamento,
                When(OrdemServicoCancelada)
                    .Then(ctx => ctx.Saga.UltimoMotivo = ctx.Message.Motivo)
                    .Publish(ctx => new CancelarOrcamento(ctx.Saga.CorrelationId, ctx.Message.Motivo))
                    .TransitionTo(Cancelada));

            // 5. Execução: finalizou -> OS finalizada | falhou ou OS cancelada -> estorna o pagamento
            During(EmExecucao,
                When(ExecucaoFinalizada)
                    .ThenAsync(ctx => Acoes(ctx).FinalizarAsync(ctx.Saga.CorrelationId, ctx.CancellationToken))
                    .TransitionTo(Finalizada),
                When(ExecucaoFalhou)
                    .Then(ctx => ctx.Saga.UltimoMotivo = $"Falha na execução: {ctx.Message.Motivo}")
                    .Publish(ctx => new EstornarPagamento(ctx.Saga.CorrelationId, ctx.Saga.UltimoMotivo!))
                    .TransitionTo(Estornando),
                When(OrdemServicoCancelada)
                    .Then(ctx => ctx.Saga.UltimoMotivo = ctx.Message.Motivo)
                    .Publish(ctx => new CancelarExecucao(ctx.Saga.CorrelationId, ctx.Message.Motivo))
                    .Publish(ctx => new EstornarPagamento(ctx.Saga.CorrelationId, ctx.Message.Motivo))
                    .TransitionTo(Estornando));

            // 6. Estorno confirmado -> OS cancelada (no-op se já estava, ex.: cancelamento pelo staff)
            During(Estornando,
                When(PagamentoEstornado)
                    .ThenAsync(ctx => Acoes(ctx).CancelarAsync(ctx.Saga.CorrelationId, $"{ctx.Saga.UltimoMotivo} - pagamento estornado", ctx.CancellationToken))
                    .TransitionTo(Cancelada));

            // Orçamento que chegou depois do cancelamento (filas diferentes não garantem ordem): cancela de novo.
            During(Cancelada,
                When(OrcamentoGerado)
                    .Publish(ctx => new CancelarOrcamento(ctx.Saga.CorrelationId, ctx.Saga.UltimoMotivo ?? "OS cancelada")));
        }

        private void Correlacionar<T>(System.Linq.Expressions.Expression<Func<Event<T>>> evento, Func<T, Guid> id) where T : class =>
            Event(evento, e =>
            {
                e.CorrelateById(m => id(m.Message));
                e.OnMissingInstance(m => m.Discard());
            });

        private static SagaOrdemServicoAcoes Acoes(ConsumeContext ctx) =>
            ctx.GetServiceOrCreateInstance<SagaOrdemServicoAcoes>();

        private static async Task PublicarGerarOrcamento(BehaviorContext<OrdemServicoSagaState, DiagnosticoConcluido> ctx)
        {
            var comando = await Acoes(ctx).MontarGerarOrcamentoAsync(ctx.Saga.CorrelationId, ctx.CancellationToken);
            await ctx.Publish(comando, ctx.CancellationToken);
        }
    }
}
