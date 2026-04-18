using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Servicos
{
    public class ExcluirServicoUseCase
    {
        private readonly IServicoRepository _servicoRepository;
        private readonly ILogger<ExcluirServicoUseCase> _logger;

        public ExcluirServicoUseCase(IServicoRepository servicoRepository, ILogger<ExcluirServicoUseCase> logger)
        {
            _servicoRepository = servicoRepository;
            _logger = logger;
        }

        public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existe = await _servicoRepository.ExistsAsync(id, cancellationToken);
            if (!existe)
            {
                _logger.LogWarning("Servico nao encontrado para exclusao: {Id}", id);
                throw new NotFoundException("Servico", id);
            }

            await _servicoRepository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Servico excluido: {Id}", id);
        }
    }
}
