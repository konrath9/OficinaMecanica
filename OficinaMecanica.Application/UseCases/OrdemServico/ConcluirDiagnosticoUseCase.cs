using MassTransit;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Contracts;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record ConcluirDiagnosticoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status, string Mensagem);

    /// <summary>
    /// Técnico conclui o diagnóstico: inicia a Saga da OS (evento DiagnosticoConcluido). O orçamento é gerado
    /// de forma assíncrona pelo Billing Service; a OS só vai para AguardandoAprovacao quando ele confirmar.
    /// </summary>
    public class ConcluirDiagnosticoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ConcluirDiagnosticoUseCase> _logger;

        public ConcluirDiagnosticoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IPublishEndpoint publishEndpoint,
            ILogger<ConcluirDiagnosticoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<ConcluirDiagnosticoResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS é obrigatório.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            try
            {
                os.SolicitarOrcamento();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            // Publicar antes de salvar: com o outbox do MassTransit a mensagem entra na mesma transação do SaveChanges.
            await _publishEndpoint.Publish(new DiagnosticoConcluido(os.Id), cancellationToken);
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("Diagnostico da OS {OsId} concluido - Saga iniciada, aguardando orcamento do Billing", ordemServicoId);

            return new ConcluirDiagnosticoResponse(os.Id, os.Numero, os.Status,
                "Diagnóstico concluído. O orçamento está sendo gerado e será enviado ao cliente para aprovação.");
        }
    }
}
