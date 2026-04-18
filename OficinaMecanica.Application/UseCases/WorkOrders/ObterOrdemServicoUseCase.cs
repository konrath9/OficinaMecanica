using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public class ObterOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<ObterOrdemServicoUseCase> _logger;

        public ObterOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IClienteRepository clienteRepository,
            IVeiculoRepository veiculoRepository,
            ILogger<ObterOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _clienteRepository = clienteRepository;
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task<OrdemServicoResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var os = await _ordemServicoRepository.GetByIdAsync(id, cancellationToken);
            if (os is null)
            {
                _logger.LogWarning("OS nao encontrada: {Id}", id);
                throw new NotFoundException("OrdemServico", id);
            }

            var cliente = await _clienteRepository.GetByIdAsync(os.ClienteId, cancellationToken);
            var veiculo = await _veiculoRepository.GetByIdAsync(os.VeiculoId, cancellationToken);

            return new OrdemServicoResponse
            {
                Id = os.Id,
                Numero = os.Numero,
                Status = os.Status,
                StatusDescricao = os.Status.ToString(),
                ClienteId = os.ClienteId,
                NomeCliente = cliente?.Nome ?? string.Empty,
                VeiculoId = os.VeiculoId,
                DescricaoVeiculo = veiculo is not null ? $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano} - {veiculo.Placa}" : string.Empty,
                Observacoes = os.Observacoes,
                TotalServicos = os.TotalServicos,
                TotalPecas = os.TotalPecas,
                TotalOrcamento = os.TotalOrcamento,
                CriadaEm = os.CreatedAt,
                IniciadaEm = os.IniciadaEm,
                FinalizadaEm = os.FinalizadaEm,
                EntregueEm = os.EntregueEm
            };
        }

        public async Task<IEnumerable<OrdemServicoResponse>> ListarAsync(CancellationToken cancellationToken = default)
        {
            var ordens = await _ordemServicoRepository.GetAllAsync(cancellationToken);

            var responses = new List<OrdemServicoResponse>();
            foreach (var os in ordens)
            {
                var cliente = await _clienteRepository.GetByIdAsync(os.ClienteId, cancellationToken);
                var veiculo = await _veiculoRepository.GetByIdAsync(os.VeiculoId, cancellationToken);

                responses.Add(new OrdemServicoResponse
                {
                    Id = os.Id,
                    Numero = os.Numero,
                    Status = os.Status,
                    StatusDescricao = os.Status.ToString(),
                    ClienteId = os.ClienteId,
                    NomeCliente = cliente?.Nome ?? string.Empty,
                    VeiculoId = os.VeiculoId,
                    DescricaoVeiculo = veiculo is not null ? $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano} - {veiculo.Placa}" : string.Empty,
                    Observacoes = os.Observacoes,
                    TotalServicos = os.TotalServicos,
                    TotalPecas = os.TotalPecas,
                    TotalOrcamento = os.TotalOrcamento,
                    CriadaEm = os.CreatedAt,
                    IniciadaEm = os.IniciadaEm,
                    FinalizadaEm = os.FinalizadaEm,
                    EntregueEm = os.EntregueEm
                });
            }

            return responses;
        }
    }
}
