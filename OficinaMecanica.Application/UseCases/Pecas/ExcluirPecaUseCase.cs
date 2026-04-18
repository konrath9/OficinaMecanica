using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Pecas
{
    public class ExcluirPecaUseCase
    {
        private readonly IPecaRepository _pecaRepository;
        private readonly ILogger<ExcluirPecaUseCase> _logger;

        public ExcluirPecaUseCase(IPecaRepository pecaRepository, ILogger<ExcluirPecaUseCase> logger)
        {
            _pecaRepository = pecaRepository;
            _logger = logger;
        }

        public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existe = await _pecaRepository.ExistsAsync(id, cancellationToken);
            if (!existe)
            {
                _logger.LogWarning("Peca nao encontrada para exclusao: {Id}", id);
                throw new NotFoundException("Peca", id);
            }

            await _pecaRepository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Peca excluida: {Id}", id);
        }
    }
}
