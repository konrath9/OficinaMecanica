using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public class CriarOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IGeradorNumeroOrdemServico _numeroGenerator;
        private readonly ILogger<CriarOrdemServicoUseCase> _logger;

        public CriarOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IClienteRepository clienteRepository,
            IVeiculoRepository veiculoRepository,
            IGeradorNumeroOrdemServico numeroGenerator,
            ILogger<CriarOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository ?? throw new ArgumentNullException(nameof(ordemServicoRepository));
            _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
            _veiculoRepository = veiculoRepository ?? throw new ArgumentNullException(nameof(veiculoRepository));
            _numeroGenerator = numeroGenerator ?? throw new ArgumentNullException(nameof(numeroGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CriarOrdemServicoResponse> HandleAsync(
            CriarOrdemServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Criando OS para Cliente: {ClienteId}, Veiculo: {VeiculoId}",
                request.ClienteId, request.VeiculoId);

            await ValidarRequestAsync(request, cancellationToken);

            var numero = await _numeroGenerator.GerarAsync(cancellationToken);

            OrdemServico ordemServico;
            try
            {
                ordemServico = new OrdemServico(
                    numero: numero,
                    clienteId: request.ClienteId,
                    veiculoId: request.VeiculoId,
                    observacoes: request.Observacoes
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro ao criar entidade OS: {Message}", ex.Message);
                throw new ValidationException("OrdemServico", ex.Message);
            }

            var criada = await _ordemServicoRepository.AddAsync(ordemServico, cancellationToken);

            _logger.LogInformation("OS criada com sucesso. Id: {Id}, Numero: {Numero}", criada.Id, criada.Numero);

            return new CriarOrdemServicoResponse(
                id: criada.Id,
                numero: criada.Numero,
                status: criada.Status,
                criadaEm: criada.CreatedAt
            );
        }

        private async Task ValidarRequestAsync(CriarOrdemServicoRequest request, CancellationToken cancellationToken)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.ClienteId == Guid.Empty)
            {
                errors.Add(nameof(request.ClienteId), new[] { "Id do cliente e obrigatorio." });
            }
            else
            {
                var clienteExiste = await _clienteRepository.ExistsAsync(request.ClienteId, cancellationToken);
                if (!clienteExiste)
                {
                    _logger.LogWarning("Cliente nao encontrado: {ClienteId}", request.ClienteId);
                    throw new NotFoundException("Cliente", request.ClienteId);
                }
            }

            if (request.VeiculoId == Guid.Empty)
            {
                errors.Add(nameof(request.VeiculoId), new[] { "Id do veiculo e obrigatorio." });
            }
            else
            {
                var veiculoExiste = await _veiculoRepository.ExistsAsync(request.VeiculoId, cancellationToken);
                if (!veiculoExiste)
                {
                    _logger.LogWarning("Veiculo nao encontrado: {VeiculoId}", request.VeiculoId);
                    throw new NotFoundException("Veiculo", request.VeiculoId);
                }
            }

            if (errors.Any())
                throw new ValidationException(errors);
        }
    }
}
