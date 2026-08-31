using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record RegistrarEntregaResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status, DateTime EntregueEm);

    public class RegistrarEntregaUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IEmailService _emailService;
        private readonly OrdemServicoMetrics _metrics;
        private readonly ILogger<RegistrarEntregaUseCase> _logger;

        public RegistrarEntregaUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IClienteRepository clienteRepository,
            IEmailService emailService,
            OrdemServicoMetrics metrics,
            ILogger<RegistrarEntregaUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _clienteRepository = clienteRepository;
            _emailService = emailService;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task<RegistrarEntregaResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS � obrigat�rio.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            _logger.LogInformation("Registrando entrega do ve�culo da OS {OsId}", ordemServicoId);

            try
            {
                os.Entregar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            var duracaoFinalizacao = OrdemServicoMetricsHelper.DuracaoEntre(
                os.HistoricoStatus, StatusOrdemServico.Finalizada, StatusOrdemServico.Entregue);
            if (duracaoFinalizacao.HasValue)
                _metrics.RegistrarTempoStatus("Finalizacao", duracaoFinalizacao.Value);

            _logger.LogInformation("OS {OsId} movida automaticamente para Entregue", ordemServicoId);

            await NotificarClienteAsync(os, cancellationToken);

            return new RegistrarEntregaResponse(os.Id, os.Numero, os.Status, os.EntregueEm!.Value);
        }

        private async Task NotificarClienteAsync(OficinaMecanica.Domain.Entities.OrdemServico os, CancellationToken cancellationToken)
        {
            var cliente = await _clienteRepository.GetByIdAsync(os.ClienteId, cancellationToken);
            if (string.IsNullOrWhiteSpace(cliente?.Email))
                return;

            try
            {
                await _emailService.EnviarNotificacaoStatusAsync(
                    new NotificacaoStatusOrdemServico(cliente.Email, cliente.Nome, os.Numero, os.Status),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _metrics.RegistrarErroIntegracao("email");
                _logger.LogWarning(ex, "Falha ao enviar e-mail de notificacao da OS {OsId}", os.Id);
            }
        }
    }
}
