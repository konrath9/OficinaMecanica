using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Pecas
{
    public class AtualizarPecaUseCase
    {
        private readonly IPecaRepository _pecaRepository;
        private readonly ILogger<AtualizarPecaUseCase> _logger;

        public AtualizarPecaUseCase(IPecaRepository pecaRepository, ILogger<AtualizarPecaUseCase> logger)
        {
            _pecaRepository = pecaRepository;
            _logger = logger;
        }

        public async Task<PecaResponse> HandleAsync(AtualizarPecaRequest request, CancellationToken cancellationToken = default)
        {
            var peca = await _pecaRepository.GetByIdAsync(request.Id, cancellationToken);
            if (peca is null)
            {
                _logger.LogWarning("Peca nao encontrada para atualizacao: {Id}", request.Id);
                throw new NotFoundException("Peca", request.Id);
            }

            // Atualiza código se informado
            if (!string.IsNullOrWhiteSpace(request.Codigo))
            {
                var comMesmoCodigo = await _pecaRepository.GetByCodigoAsync(request.Codigo, cancellationToken);
                if (comMesmoCodigo is not null && comMesmoCodigo.Id != request.Id)
                    throw new ValidationException("Codigo", $"Já existe outra peça com o código '{request.Codigo.ToUpperInvariant()}'.");

                try
                {
                    peca.AtualizarCodigo(request.Codigo);
                }
                catch (ArgumentException ex)
                {
                    throw new ValidationException("Codigo", ex.Message);
                }
            }

            try
            {
                peca.Atualizar(request.Nome, request.PrecoUnitario);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Peca", ex.Message);
            }

            await _pecaRepository.UpdateAsync(peca, cancellationToken);
            _logger.LogInformation("Peca atualizada: {Id}", request.Id);

            return CriarPecaUseCase.MapToResponse(peca);
        }
    }
}
