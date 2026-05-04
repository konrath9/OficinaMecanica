using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Veiculos;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Veiculos
{
    public class ObterVeiculoUseCase
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<ObterVeiculoUseCase> _logger;

        public ObterVeiculoUseCase(IVeiculoRepository veiculoRepository, ILogger<ObterVeiculoUseCase> logger)
        {
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task<VeiculoResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var veiculo = await _veiculoRepository.GetByIdAsync(id, cancellationToken);
            if (veiculo is null)
            {
                _logger.LogWarning("Veiculo nao encontrado: {Id}", id);
                throw new NotFoundException("Veiculo", id);
            }

            return CriarVeiculoUseCase.MapToResponse(veiculo, veiculo.Cliente?.Nome);
        }

        public async Task<IEnumerable<VeiculoResponse>> ListarAsync(CancellationToken cancellationToken = default)
        {
            var veiculos = await _veiculoRepository.GetAllAsync(cancellationToken);
            return veiculos.Select(v => CriarVeiculoUseCase.MapToResponse(v, v.Cliente?.Nome));
        }

        public async Task<IEnumerable<VeiculoResponse>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            var veiculos = await _veiculoRepository.GetByClienteIdAsync(clienteId, cancellationToken);
            return veiculos.Select(v => CriarVeiculoUseCase.MapToResponse(v, v.Cliente?.Nome));
        }
    }
}
