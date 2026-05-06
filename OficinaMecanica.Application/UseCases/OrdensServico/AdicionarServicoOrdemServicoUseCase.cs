using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public class AdicionarServicoOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IServicoRepository _servicoRepository;
        private readonly ILogger<AdicionarServicoOrdemServicoUseCase> _logger;

        public AdicionarServicoOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IServicoRepository servicoRepository,
            ILogger<AdicionarServicoOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository ?? throw new ArgumentNullException(nameof(ordemServicoRepository));
            _servicoRepository = servicoRepository ?? throw new ArgumentNullException(nameof(servicoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AdicionarServicoOrdemServicoResponse> HandleAsync(
            AdicionarServicoOrdemServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Adicionando servico a OS: {OrdemServicoId}", request.OrdemServicoId);

            Validate(request);

            var ordemServico = await _ordemServicoRepository.GetByIdAsync(request.OrdemServicoId, cancellationToken);
            if (ordemServico == null)
            {
                _logger.LogWarning("OS nao encontrada: {OrdemServicoId}", request.OrdemServicoId);
                throw new NotFoundException("OrdemServico", request.OrdemServicoId);
            }

            var servico = await _servicoRepository.GetByIdAsync(request.ServicoId, cancellationToken);
            if (servico is null)
            {
                _logger.LogWarning("Servico nao encontrado: {ServicoId}", request.ServicoId);
                throw new NotFoundException("Servico", request.ServicoId);
            }

            ItemServico itemServico;
            try
            {
                itemServico = new ItemServico(
                    servicoId: servico.Id,
                    descricao: servico.Nome,
                    precoUnitario: servico.Preco,
                    quantidade: request.Quantidade
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Item de servico invalido para OS {OrdemServicoId}: {Message}",
                    request.OrdemServicoId, ex.Message);
                throw new ValidationException("ItemServico", ex.Message);
            }

            try
            {
                ordemServico.AdicionarServico(itemServico);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Nao foi possivel adicionar servico a OS {OrdemServicoId}: {Message}",
                    request.OrdemServicoId, ex.Message);
                throw new ValidationException("OrdemServico", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

            _logger.LogInformation("Servico adicionado com sucesso a OS: {OrdemServicoId}", request.OrdemServicoId);

            return new AdicionarServicoOrdemServicoResponse(ordemServico.Id);
        }

        private static void Validate(AdicionarServicoOrdemServicoRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.OrdemServicoId == Guid.Empty)
                errors.Add(nameof(request.OrdemServicoId), new[] { "Id da OS e obrigatorio." });

            if (request.ServicoId == Guid.Empty)
                errors.Add(nameof(request.ServicoId), new[] { "Id do servico e obrigatorio." });

            if (request.Quantidade <= 0)
                errors.Add(nameof(request.Quantidade), new[] { "Quantidade deve ser maior que zero." });

            if (errors.Any())
                throw new ValidationException(errors);
        }
    }
}
