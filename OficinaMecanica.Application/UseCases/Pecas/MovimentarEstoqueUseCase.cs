using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Pecas
{
    public class MovimentarEstoqueUseCase
    {
        private readonly IPecaRepository _pecaRepository;
        private readonly ILogger<MovimentarEstoqueUseCase> _logger;

        public MovimentarEstoqueUseCase(IPecaRepository pecaRepository, ILogger<MovimentarEstoqueUseCase> logger)
        {
            _pecaRepository = pecaRepository;
            _logger = logger;
        }

        public async Task<PecaResponse> HandleAsync(MovimentarEstoqueRequest request, CancellationToken cancellationToken = default)
        {
            var tipoNormalizado = request.Tipo?.Trim().ToLowerInvariant();
            if (tipoNormalizado is not "entrada" and not "saida")
                throw new ValidationException("Tipo", "Tipo de movimentação inválido. Use 'entrada' ou 'saida'.");

            if (request.Quantidade <= 0)
                throw new ValidationException("Quantidade", "Quantidade deve ser maior que zero.");

            var peca = await _pecaRepository.GetByIdAsync(request.Id, cancellationToken);
            if (peca is null)
                throw new NotFoundException($"Peça com Id {request.Id} não encontrada.");

            try
            {
                if (tipoNormalizado == "entrada")
                {
                    peca.EntradaEstoque(request.Quantidade);
                    _logger.LogInformation("Entrada de {Qtd} unidade(s) na peça {Id}", request.Quantidade, request.Id);
                }
                else
                {
                    peca.SaidaEstoque(request.Quantidade);
                    _logger.LogInformation("Saída de {Qtd} unidade(s) da peça {Id}", request.Quantidade, request.Id);
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("Estoque", ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Quantidade", ex.Message);
            }

            await _pecaRepository.UpdateAsync(peca, cancellationToken);

            return CriarPecaUseCase.MapToResponse(peca);
        }
    }
}
