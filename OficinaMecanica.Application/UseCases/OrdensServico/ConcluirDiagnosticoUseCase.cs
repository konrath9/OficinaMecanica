using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record ConcluirDiagnosticoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    public class ConcluirDiagnosticoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<ConcluirDiagnosticoUseCase> _logger;

        public ConcluirDiagnosticoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<ConcluirDiagnosticoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _logger = logger;
        }

        public async Task<ConcluirDiagnosticoResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS é obrigatório.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            _logger.LogInformation("Técnico concluiu diagnóstico da OS {OsId} — enviando para aprovação do cliente", ordemServicoId);

            try
            {
                os.EnviarParaAprovacao();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("OS {OsId} movida automaticamente para AguardandoAprovacao", ordemServicoId);

            return new ConcluirDiagnosticoResponse(os.Id, os.Numero, os.Status);
        }
    }
}
