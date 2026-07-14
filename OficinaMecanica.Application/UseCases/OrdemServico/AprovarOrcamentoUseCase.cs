using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record AprovarOrcamentoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    public class AprovarOrcamentoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<AprovarOrcamentoUseCase> _logger;

        public AprovarOrcamentoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<AprovarOrcamentoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _logger = logger;
        }

        public async Task<AprovarOrcamentoResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS é obrigatório.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            _logger.LogInformation("Cliente aprovou orçamento da OS {OsId} — iniciando execução", ordemServicoId);

            try
            {
                os.Aprovar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("OS {OsId} movida automaticamente para EmExecucao após aprovação do cliente", ordemServicoId);

            return new AprovarOrcamentoResponse(os.Id, os.Numero, os.Status);
        }
    }
}
