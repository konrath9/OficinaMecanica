using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Pecas
{
    public class ObterPecaUseCase
    {
        private readonly IPecaRepository _pecaRepository;
        private readonly ILogger<ObterPecaUseCase> _logger;

        public ObterPecaUseCase(IPecaRepository pecaRepository, ILogger<ObterPecaUseCase> logger)
        {
            _pecaRepository = pecaRepository;
            _logger = logger;
        }

        public async Task<PecaResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var peca = await _pecaRepository.GetByIdAsync(id, cancellationToken);
            if (peca is null)
            {
                _logger.LogWarning("Peca nao encontrada: {Id}", id);
                throw new NotFoundException("Peca", id);
            }

            return CriarPecaUseCase.MapToResponse(peca);
        }

        public async Task<IEnumerable<PecaResponse>> ListarAsync(CancellationToken cancellationToken = default)
        {
            var pecas = await _pecaRepository.GetAllAsync(cancellationToken);
            return pecas.Select(CriarPecaUseCase.MapToResponse);
        }

        public async Task<IEnumerable<PecaResponse>> ListarSemEstoqueAsync(CancellationToken cancellationToken = default)
        {
            var pecas = await _pecaRepository.GetSemEstoqueAsync(cancellationToken);
            return pecas.Select(CriarPecaUseCase.MapToResponse);
        }
    }
}
