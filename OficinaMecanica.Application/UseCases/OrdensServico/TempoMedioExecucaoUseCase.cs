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
            _logger.LogInformation("Calculando tempo medio de execucao por servico. Periodo: {Inicio} - {Fim}", periodoInicio, periodoFim);

            var ordens = await _ordemServicoRepository.GetFinalizadasNoPeriodoAsync(periodoInicio, periodoFim, cancellationToken);

            // Coleta todos os itens de servico com duracao registrada
            var itensFinalizados = ordens
                .SelectMany(os => os.Servicos)
                .Where(s => s.IniciadoEm.HasValue && s.FinalizadoEm.HasValue)
                .ToList();

            // Agrupa por ServicoId e calcula a media de duracao de cada tipo
            var porServico = itensFinalizados
                .GroupBy(s => new { s.ServicoId, s.Descricao })
                .Select(g => new TempoMedioPorServicoDto
                {
                    ServicoId = g.Key.ServicoId,
                    Descricao = g.Key.Descricao,
                    TempoMedioHoras = Math.Round(g.Average(s => s.DuracaoHoras!.Value), 2),
                    TempoMedioDias = Math.Round(g.Average(s => s.DuracaoHoras!.Value) / 24, 2),
                    TotalExecucoes = g.Count()
                })
                .OrderByDescending(x => x.TempoMedioHoras)
                .ToList();

            return new TempoMedioExecucaoResponse
            {
                PeriodoInicio = periodoInicio,
                PeriodoFim = periodoFim,
                TotalServicosFinalizados = itensFinalizados.Count,
                PorServico = porServico
            };
        }
    }
}
