using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Enums;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities; // Ensure this using is present

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public class AlterarStatusOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<AlterarStatusOrdemServicoUseCase> _logger;

        public AlterarStatusOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<AlterarStatusOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository ?? throw new ArgumentNullException(nameof(ordemServicoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AlterarStatusOrdemServicoResponse> HandleAsync(
            AlterarStatusOrdemServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Alterando status da OS: {OrdemServicoId}, Acao: {Acao}",
                request.OrdemServicoId, request.Acao);

            Validate(request);

            var ordemServico = await _ordemServicoRepository.GetByIdAsync(request.OrdemServicoId, cancellationToken);
            if (ordemServico == null)
            {
                _logger.LogWarning("OS nao encontrada: {OrdemServicoId}", request.OrdemServicoId);
                throw new NotFoundException("OrdemServico", request.OrdemServicoId);
            }

            try
            {
                ExecutarAcao(ordemServico, request.Acao);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Nao foi possivel executar acao {Acao} na OS {OrdemServicoId}: {Message}",
                    request.Acao, request.OrdemServicoId, ex.Message);
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

            _logger.LogInformation("Status da OS alterado com sucesso: {OrdemServicoId}", request.OrdemServicoId);

            return new AlterarStatusOrdemServicoResponse(ordemServico.Id);
        }

        private static void Validate(AlterarStatusOrdemServicoRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.OrdemServicoId == Guid.Empty)
                errors.Add(nameof(request.OrdemServicoId), new[] { "Id da OS e obrigatorio." });

            if (!Enum.IsDefined(typeof(AcaoOrdemServico), request.Acao))
                errors.Add(nameof(request.Acao), new[] { "Acao invalida." });

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private static void ExecutarAcao(Domain.Entities.OrdemServico ordemServico, AcaoOrdemServico acao)
        {
            switch (acao)
            {
                case AcaoOrdemServico.IniciarDiagnostico:
                    ordemServico.IniciarDiagnostico();
                    break;
                case AcaoOrdemServico.EnviarParaAprovacao:
                    ordemServico.EnviarParaAprovacao();
                    break;
                case AcaoOrdemServico.Aprovar:
                    ordemServico.Aprovar();
                    break;
                case AcaoOrdemServico.Finalizar:
                    ordemServico.Finalizar();
                    break;
                case AcaoOrdemServico.Entregar:
                    ordemServico.Entregar();
                    break;
                case AcaoOrdemServico.Cancelar:
                    ordemServico.Cancelar();
                    break;
                default:
                    throw new InvalidOperationException($"Acao nao suportada: {acao}");
            }
        }
    }
}
