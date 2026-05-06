using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public class AdicionarPecaOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IPecaRepository _pecaRepository;
        private readonly ILogger<AdicionarPecaOrdemServicoUseCase> _logger;

        public AdicionarPecaOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IPecaRepository pecaRepository,
            ILogger<AdicionarPecaOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository ?? throw new ArgumentNullException(nameof(ordemServicoRepository));
            _pecaRepository = pecaRepository ?? throw new ArgumentNullException(nameof(pecaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AdicionarPecaOrdemServicoResponse> HandleAsync(
            AdicionarPecaOrdemServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Adicionando peca a OS: {OrdemServicoId}", request.OrdemServicoId);

            Validate(request);

            var ordemServico = await _ordemServicoRepository.GetByIdAsync(request.OrdemServicoId, cancellationToken);
            if (ordemServico == null)
            {
                _logger.LogWarning("OS nao encontrada: {OrdemServicoId}", request.OrdemServicoId);
                throw new NotFoundException("OrdemServico", request.OrdemServicoId);
            }

            var peca = await _pecaRepository.GetByIdAsync(request.PecaId, cancellationToken);
            if (peca is null)
            {
                _logger.LogWarning("Peca nao encontrada: {PecaId}", request.PecaId);
                throw new NotFoundException("Peca", request.PecaId);
            }

            if (!peca.TemEstoque(request.Quantidade))
            {
                _logger.LogWarning("Estoque insuficiente para peca {PecaId}. Necessario: {Necessario}, Disponivel: {Disponivel}",
                    request.PecaId, request.Quantidade, peca.QuantidadeEstoque);
                throw new ValidationException("Peca", $"Estoque insuficiente. Disponivel: {peca.QuantidadeEstoque}");
            }

            ItemPeca itemPeca;
            try
            {
                itemPeca = new ItemPeca(
                    pecaId: peca.Id,
                    codigo: peca.Codigo,
                    descricao: peca.Nome,
                    precoUnitario: peca.PrecoUnitario,
                    quantidade: request.Quantidade
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Item de peca invalido para OS {OrdemServicoId}: {Message}",
                    request.OrdemServicoId, ex.Message);
                throw new ValidationException("ItemPeca", ex.Message);
            }

            try
            {
                ordemServico.AdicionarPeca(itemPeca);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Nao foi possivel adicionar peca a OS {OrdemServicoId}: {Message}",
                    request.OrdemServicoId, ex.Message);
                throw new ValidationException("OrdemServico", ex.Message);
            }

            peca.SaidaEstoque(request.Quantidade);
            await _pecaRepository.UpdateAsync(peca, cancellationToken);
            await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

            _logger.LogInformation("Peca adicionada com sucesso a OS: {OrdemServicoId}", request.OrdemServicoId);

            return new AdicionarPecaOrdemServicoResponse(ordemServico.Id);
        }

        private static void Validate(AdicionarPecaOrdemServicoRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.OrdemServicoId == Guid.Empty)
                errors.Add(nameof(request.OrdemServicoId), new[] { "Id da OS e obrigatorio." });

            if (request.PecaId == Guid.Empty)
                errors.Add(nameof(request.PecaId), new[] { "Id da peca e obrigatorio." });

            if (request.Quantidade <= 0)
                errors.Add(nameof(request.Quantidade), new[] { "Quantidade deve ser maior que zero." });

            if (errors.Any())
                throw new ValidationException(errors);
        }
    }
}
