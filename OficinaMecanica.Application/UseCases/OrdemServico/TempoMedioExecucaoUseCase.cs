using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    /// <summary>
    /// Tempo médio de execução das OS (EmExecucao -> Finalizada), calculado a partir do histórico de status.
    /// O detalhe por serviço individual passou a ser do Execution Service (Fase 4).
    /// </summary>
    public class TempoMedioExecucaoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<TempoMedioExecucaoUseCase> _logger;

        public TempoMedioExecucaoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<TempoMedioExecucaoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _logger = logger;
        }

        public async Task<TempoMedioExecucaoResponse> HandleAsync(
            DateTime? periodoInicio = null,
            DateTime? periodoFim = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calculando tempo medio de execucao. Periodo: {Inicio} - {Fim}", periodoInicio, periodoFim);

            var ordens = await _ordemServicoRepository.GetFinalizadasNoPeriodoAsync(periodoInicio, periodoFim, cancellationToken);

            var duracoes = ordens
                .Select(os => OrdemServicoMetricsHelper.DuracaoEntre(os.HistoricoStatus, StatusOrdemServico.EmExecucao, StatusOrdemServico.Finalizada))
                .Where(d => d.HasValue)
                .Select(d => d!.Value.TotalHours)
                .ToList();

            var mediaHoras = duracoes.Count == 0 ? 0 : Math.Round(duracoes.Average(), 2);

            return new TempoMedioExecucaoResponse
            {
                PeriodoInicio = periodoInicio,
                PeriodoFim = periodoFim,
                TotalOrdensFinalizadas = duracoes.Count,
                TempoMedioHoras = mediaHoras,
                TempoMedioDias = Math.Round(mediaHoras / 24, 2)
            };
        }
    }
}
