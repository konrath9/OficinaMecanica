using MassTransit;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Contracts;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record AprovarOrcamentoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    /// <summary>
    /// Cliente aprova o orçamento: a OS vai para AguardandoPagamento e a Saga pede ao Billing a cobrança.
    /// A execução só começa quando o pagamento for confirmado (PagamentoAprovado).
    /// </summary>
    public class AprovarOrcamentoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly NotificadorStatusOrdemServico _notificador;
        private readonly ILogger<AprovarOrcamentoUseCase> _logger;

        public AprovarOrcamentoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IPublishEndpoint publishEndpoint,
            NotificadorStatusOrdemServico notificador,
            ILogger<AprovarOrcamentoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _publishEndpoint = publishEndpoint;
            _notificador = notificador;
            _logger = logger;
        }

        public async Task<AprovarOrcamentoResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS é obrigatório.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            try
            {
                os.Aprovar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _publishEndpoint.Publish(new OrcamentoAprovadoPeloCliente(os.Id), cancellationToken);
            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("Cliente aprovou o orcamento da OS {OsId} - aguardando pagamento", ordemServicoId);

            await _notificador.NotificarAsync(os, cancellationToken: cancellationToken);

            return new AprovarOrcamentoResponse(os.Id, os.Numero, os.Status);
        }
    }
}
