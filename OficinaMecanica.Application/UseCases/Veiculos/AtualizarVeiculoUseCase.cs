using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Veiculos;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Veiculos
{
    public class AtualizarVeiculoUseCase
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<AtualizarVeiculoUseCase> _logger;

        public AtualizarVeiculoUseCase(IVeiculoRepository veiculoRepository, ILogger<AtualizarVeiculoUseCase> logger)
        {
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task<VeiculoResponse> HandleAsync(AtualizarVeiculoRequest request, CancellationToken cancellationToken = default)
        {
            var veiculo = await _veiculoRepository.GetByIdAsync(request.Id, cancellationToken);
            if (veiculo is null)
            {
                _logger.LogWarning("Veiculo nao encontrado para atualizacao: {Id}", request.Id);
                throw new NotFoundException("Veiculo", request.Id);
            }

            try
            {
                veiculo.Atualizar(request.Marca, request.Modelo, request.Ano);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Veiculo", ex.Message);
            }

            await _veiculoRepository.UpdateAsync(veiculo, cancellationToken);
            _logger.LogInformation("Veiculo atualizado: {Id}", request.Id);

            return CriarVeiculoUseCase.MapToResponse(veiculo, veiculo.Cliente?.Nome);
        }
    }
}
