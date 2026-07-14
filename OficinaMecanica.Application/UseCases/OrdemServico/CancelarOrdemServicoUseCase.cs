using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record CancelarOrdemServicoRequest(Guid OrdemServicoId, string? Motivo = null);
    public record CancelarOrdemServicoResponse(Guid OrdemServicoId, string Numero, StatusOrdemServico Status);

    public class CancelarOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<CancelarOrdemServicoUseCase> _logger;

        public CancelarOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IClienteRepository clienteRepository,
            IEmailService emailService,
            ILogger<CancelarOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _clienteRepository = clienteRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<CancelarOrdemServicoResponse> HandleAsync(
            CancelarOrdemServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.OrdemServicoId == Guid.Empty)
                throw new ValidationException("OrdemServicoId", "Id da OS � obrigat�rio.");

            var os = await _ordemServicoRepository.GetByIdAsync(request.OrdemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", request.OrdemServicoId);

            _logger.LogInformation("Cancelando OS {OsId}. Motivo: {Motivo}", request.OrdemServicoId, request.Motivo ?? "n�o informado");

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

            await NotificarClienteAsync(os, request.Motivo, cancellationToken);

            return new CancelarOrdemServicoResponse(os.Id, os.Numero, os.Status);
        }

        private async Task NotificarClienteAsync(OficinaMecanica.Domain.Entities.OrdemServico os, string? motivo, CancellationToken cancellationToken)
        {
            var cliente = await _clienteRepository.GetByIdAsync(os.ClienteId, cancellationToken);
            if (string.IsNullOrWhiteSpace(cliente?.Email))
                return;

            try
            {
                await _emailService.EnviarNotificacaoStatusAsync(
                    new NotificacaoStatusOrdemServico(cliente.Email, cliente.Nome, os.Numero, os.Status, motivo),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar e-mail de notificacao da OS {OsId}", os.Id);
            }
        }
    }
}
