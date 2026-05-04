using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
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

            var ordensComTempo = ordens
                .Where(os => os.IniciadaEm.HasValue && os.FinalizadaEm.HasValue)
                .ToList();

            double tempoMedioHoras = 0;
            if (ordensComTempo.Any())
            {
                tempoMedioHoras = ordensComTempo
                    .Average(os => (os.FinalizadaEm!.Value - os.IniciadaEm!.Value).TotalHours);
            }

            return new TempoMedioExecucaoResponse
            {
                TempoMedioHoras = Math.Round(tempoMedioHoras, 2),
                TempoMedioDias = Math.Round(tempoMedioHoras / 24, 2),
                TotalOrdensFinalizadas = ordensComTempo.Count,
                PeriodoInicio = periodoInicio,
                PeriodoFim = periodoFim
            };
        }
    }
}
