using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Clientes
{
    public class AtualizarStatusClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<AtualizarStatusClienteUseCase> _logger;

        public AtualizarStatusClienteUseCase(IClienteRepository clienteRepository, ILogger<AtualizarStatusClienteUseCase> logger)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task<ClienteResponse> HandleAsync(Guid id, bool ativo, CancellationToken cancellationToken = default)
        {
            var cliente = await _clienteRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Cliente", id);

            try
            {
                if (ativo)
                    cliente.Ativar();
                else
                    cliente.Desativar();
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("Ativo", ex.Message);
            }

            await _clienteRepository.UpdateAsync(cliente, cancellationToken);
            _logger.LogInformation("Status do cliente {Id} atualizado para Ativo={Ativo}", id, ativo);

            return CriarClienteUseCase.MapToResponse(cliente);
        }
    }
}
