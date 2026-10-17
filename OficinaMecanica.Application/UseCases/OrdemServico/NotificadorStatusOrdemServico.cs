using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    /// <summary>
    /// Envia o e-mail de mudança de status ao cliente dono da OS. Falha de e-mail nunca derruba o fluxo
    /// (nem a Saga): só registra a métrica integracoes.erros e um warning.
    /// </summary>
    public class NotificadorStatusOrdemServico
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IEmailService _emailService;
        private readonly OrdemServicoMetrics _metrics;
        private readonly ILogger<NotificadorStatusOrdemServico> _logger;

        public NotificadorStatusOrdemServico(
            IClienteRepository clienteRepository,
            IEmailService emailService,
            OrdemServicoMetrics metrics,
            ILogger<NotificadorStatusOrdemServico> logger)
        {
            _clienteRepository = clienteRepository;
            _emailService = emailService;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task NotificarAsync(Domain.Entities.OrdemServico os, string? observacao = null, CancellationToken cancellationToken = default)
        {
            var cliente = await _clienteRepository.GetByIdAsync(os.ClienteId, cancellationToken);
            if (string.IsNullOrWhiteSpace(cliente?.Email))
                return;

            try
            {
                await _emailService.EnviarNotificacaoStatusAsync(
                    new NotificacaoStatusOrdemServico(cliente.Email, cliente.Nome, os.Numero, os.Status, observacao),
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
