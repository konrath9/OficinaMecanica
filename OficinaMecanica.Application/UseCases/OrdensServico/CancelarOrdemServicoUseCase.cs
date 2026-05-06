using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record CancelarOrdemServicoRequest(Guid OrdemServicoId, string? Motivo = null);
    public record CancelarOrdemServicoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    public class CancelarOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<CancelarOrdemServicoUseCase> _logger;

        public CancelarOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<CancelarOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
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

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("OS {OsId} cancelada com sucesso", request.OrdemServicoId);

            return new CancelarOrdemServicoResponse(os.Id, os.Numero, os.Status);
        }
    }
}
