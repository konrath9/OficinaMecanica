using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record RegistrarEntregaResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status, DateTime EntregueEm);

    public class RegistrarEntregaUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<RegistrarEntregaUseCase> _logger;

        public RegistrarEntregaUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<RegistrarEntregaUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _logger = logger;
        }

        public async Task<RegistrarEntregaResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS é obrigatório.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            _logger.LogInformation("Registrando entrega do veículo da OS {OsId}", ordemServicoId);

            try
            {
                os.Entregar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("OS {OsId} movida automaticamente para Entregue", ordemServicoId);

            return new RegistrarEntregaResponse(os.Id, os.Numero, os.Status, os.EntregueEm!.Value);
        }
    }
}
