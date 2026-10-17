using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Contracts;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    /// <summary>
    /// Efeitos locais (na OS) de cada passo da Saga. A state machine (Infrastructure/Mensageria) decide
    /// QUANDO cada um roda e quais comandos publicar; aqui fica O QUE muda na OS - testável sem broker.
    /// Todos os métodos são idempotentes: a Saga pode reprocessar uma mensagem (entrega at-least-once).
    /// </summary>
    public class SagaOrdemServicoAcoes
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly NotificadorStatusOrdemServico _notificador;
        private readonly OrdemServicoMetrics _metrics;
        private readonly ILogger<SagaOrdemServicoAcoes> _logger;

        public SagaOrdemServicoAcoes(
            IOrdemServicoRepository ordemServicoRepository,
            IClienteRepository clienteRepository,
            NotificadorStatusOrdemServico notificador,
            OrdemServicoMetrics metrics,
            ILogger<SagaOrdemServicoAcoes> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _clienteRepository = clienteRepository;
            _notificador = notificador;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task<GerarOrcamento> MontarGerarOrcamentoAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            var cliente = await _clienteRepository.GetByIdAsync(os.ClienteId, cancellationToken);

            var itens = os.Servicos
                .Select(s => new ItemOrcamento(s.Descricao, "Servico", s.Quantidade, s.PrecoUnitario))
                .Concat(os.Pecas.Select(p => new ItemOrcamento(p.Descricao, "Peca", p.Quantidade, p.PrecoUnitario)))
                .ToList();

            return new GerarOrcamento(os.Id, os.Numero, os.ClienteId, cliente?.Email, itens, os.TotalOrcamento);
        }

        public async Task EnviarParaAprovacaoAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            if (os.Status != StatusOrdemServico.EmDiagnostico)
                return;

            os.EnviarParaAprovacao();
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            var duracao = OrdemServicoMetricsHelper.DuracaoEntre(
                os.HistoricoStatus, StatusOrdemServico.EmDiagnostico, StatusOrdemServico.AguardandoAprovacao);
            if (duracao.HasValue)
                _metrics.RegistrarTempoStatus("Diagnostico", duracao.Value);

            await _notificador.NotificarAsync(os, cancellationToken: cancellationToken);
        }

        public async Task RegistrarFalhaOrcamentoAsync(Guid ordemServicoId, string motivo, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            os.RegistrarFalhaOrcamento(motivo);
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);
            _metrics.RegistrarErroIntegracao("billing");
            _logger.LogWarning("Billing nao gerou o orcamento da OS {OsId}: {Motivo}", ordemServicoId, motivo);
        }

        public async Task RegistrarLinkPagamentoAsync(Guid ordemServicoId, string? link, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            os.RegistrarLinkPagamento(link);
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);
        }

        public async Task<IniciarExecucao> ConfirmarPagamentoAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            if (os.Status == StatusOrdemServico.AguardandoPagamento)
            {
                os.ConfirmarPagamento();
                await _ordemServicoRepository.UpdateAsync(os, cancellationToken);
                await _notificador.NotificarAsync(os, cancellationToken: cancellationToken);
            }

            return new IniciarExecucao(
                os.Id,
                os.Numero,
                os.Servicos.Select(s => new ServicoExecucao(s.ServicoId, s.Descricao, s.Quantidade)).ToList());
        }

        public async Task FinalizarAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            if (os.Status != StatusOrdemServico.EmExecucao)
                return;

            os.Finalizar();
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            var duracao = OrdemServicoMetricsHelper.DuracaoEntre(
                os.HistoricoStatus, StatusOrdemServico.EmExecucao, StatusOrdemServico.Finalizada);
            if (duracao.HasValue)
                _metrics.RegistrarTempoStatus("Execucao", duracao.Value);

            await _notificador.NotificarAsync(os, cancellationToken: cancellationToken);
        }

        public async Task CancelarAsync(Guid ordemServicoId, string motivo, CancellationToken cancellationToken = default)
        {
            var os = await ObterAsync(ordemServicoId, cancellationToken);
            if (os.Status is StatusOrdemServico.Cancelada or StatusOrdemServico.Finalizada or StatusOrdemServico.Entregue)
                return;

            os.Cancelar(motivo);
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);
            _logger.LogWarning("OS {OsId} cancelada pela Saga (compensacao): {Motivo}", ordemServicoId, motivo);
            await _notificador.NotificarAsync(os, motivo, cancellationToken);
        }

        private async Task<Domain.Entities.OrdemServico> ObterAsync(Guid ordemServicoId, CancellationToken cancellationToken) =>
            await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);
    }
}
