using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record AprovarOrcamentoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    public class AprovarOrcamentoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AprovarOrcamentoUseCase> _logger;

        public AprovarOrcamentoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IClienteRepository clienteRepository,
            IEmailService emailService,
            ILogger<AprovarOrcamentoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _clienteRepository = clienteRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AprovarOrcamentoResponse> HandleAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
        {
            if (ordemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS � obrigat�rio.");

            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", ordemServicoId);

            _logger.LogInformation("Cliente aprovou or�amento da OS {OsId} � iniciando execu��o", ordemServicoId);

            try
            {
                os.Aprovar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            _logger.LogInformation("OS {OsId} movida automaticamente para EmExecucao ap�s aprova��o do cliente", ordemServicoId);

            await NotificarClienteAsync(os, cancellationToken);

            return new AprovarOrcamentoResponse(os.Id, os.Numero, os.Status);
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
                _logger.LogWarning(ex, "Falha ao enviar e-mail de notificacao da OS {OsId}", os.Id);
            }
        }
    }
}
