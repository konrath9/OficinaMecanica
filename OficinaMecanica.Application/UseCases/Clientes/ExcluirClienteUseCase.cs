using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Clientes
{
    public class ExcluirClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<ExcluirClienteUseCase> _logger;

        public ExcluirClienteUseCase(IClienteRepository clienteRepository, ILogger<ExcluirClienteUseCase> logger)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
        }

        public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existe = await _clienteRepository.ExistsAsync(id, cancellationToken);
            if (!existe)
            {
                _logger.LogWarning("Cliente nao encontrado para exclusao: {Id}", id);
                throw new NotFoundException("Cliente", id);
            }

            await _clienteRepository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Cliente excluido: {Id}", id);
        }
    }
}
