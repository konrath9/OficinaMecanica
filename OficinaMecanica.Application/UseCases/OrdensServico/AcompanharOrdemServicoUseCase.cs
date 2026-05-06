using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    /// <summary>
    /// Endpoint público: qualquer pessoa pode acompanhar o status da OS pelo número, sem autenticação.
    /// Retorna apenas dados não sensíveis.
    /// </summary>
    public class AcompanharOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<AcompanharOrdemServicoUseCase> _logger;

        public AcompanharOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IVeiculoRepository veiculoRepository,
            ILogger<AcompanharOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task<AcompanhamentoOrdemServicoResponse> HandleAsync(string numero, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ValidationException("Numero", "Número da OS é obrigatório.");

            _logger.LogInformation("Acompanhamento público da OS: {Numero}", numero);

            var os = await _ordemServicoRepository.GetByNumeroAsync(numero, cancellationToken);
            if (os is null)
            {
                _logger.LogWarning("OS nao encontrada para acompanhamento: {Numero}", numero);
                throw new NotFoundException("OrdemServico", numero);
            }

            var veiculo = await _veiculoRepository.GetByIdAsync(os.VeiculoId, cancellationToken);

            return new AcompanhamentoOrdemServicoResponse
            {
                Numero = os.Numero,
                Status = os.Status,
                StatusDescricao = os.Status.ToString(),
                DescricaoVeiculo = veiculo is not null ? $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano} - {veiculo.Placa}" : string.Empty,
                Observacoes = os.Observacoes,
                TotalOrcamento = os.TotalOrcamento,
                CriadaEm = os.CreatedAt,
                IniciadaEm = os.IniciadaEm,
                FinalizadaEm = os.FinalizadaEm,
                EntregueEm = os.EntregueEm
            };
        }
    }
}
