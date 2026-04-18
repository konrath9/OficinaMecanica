using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Veiculos
{
    public class ExcluirVeiculoUseCase
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<ExcluirVeiculoUseCase> _logger;

        public ExcluirVeiculoUseCase(IVeiculoRepository veiculoRepository, ILogger<ExcluirVeiculoUseCase> logger)
        {
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existe = await _veiculoRepository.ExistsAsync(id, cancellationToken);
            if (!existe)
            {
                _logger.LogWarning("Veiculo nao encontrado para exclusao: {Id}", id);
                throw new NotFoundException("Veiculo", id);
            }

            await _veiculoRepository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Veiculo excluido: {Id}", id);
        }
    }
}
