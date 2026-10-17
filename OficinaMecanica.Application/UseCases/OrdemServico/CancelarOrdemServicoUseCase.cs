using MassTransit;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Contracts;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record CancelarOrdemServicoRequest(Guid OrdemServicoId, string? Motivo = null);
    public record CancelarOrdemServicoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    /// <summary>
    /// Cancela a OS (staff, ou cliente reprovando o orçamento). Publica OrdemServicoCancelada para a Saga
    /// compensar o que já aconteceu nos outros serviços (cancelar orçamento, estornar pagamento, tirar da fila).
    /// </summary>
    public class CancelarOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly NotificadorStatusOrdemServico _notificador;
        private readonly ILogger<CancelarOrdemServicoUseCase> _logger;

        public CancelarOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IPublishEndpoint publishEndpoint,
            NotificadorStatusOrdemServico notificador,
            ILogger<CancelarOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _publishEndpoint = publishEndpoint;
            _notificador = notificador;
            _logger = logger;
        }

        public async Task<CancelarOrdemServicoResponse> HandleAsync(
            CancelarOrdemServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.OrdemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS é obrigatório.");

            var os = await _ordemServicoRepository.GetByIdAsync(request.OrdemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", request.OrdemServicoId);

            _logger.LogInformation("Cancelando OS {OsId}. Motivo: {Motivo}", request.OrdemServicoId, request.Motivo ?? "não informado");

            try
            {
                os.Cancelar(request.Motivo);
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _publishEndpoint.Publish(new OrdemServicoCancelada(os.Id, request.Motivo ?? "Cancelada"), cancellationToken);
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("OS {OsId} cancelada com sucesso", request.OrdemServicoId);

            await _notificador.NotificarAsync(os, request.Motivo, cancellationToken);

            return new CancelarOrdemServicoResponse(os.Id, os.Numero, os.Status);
        }
    }
}
