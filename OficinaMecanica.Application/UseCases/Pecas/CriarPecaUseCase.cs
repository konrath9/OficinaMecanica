using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.Pecas
{
    public class CriarPecaUseCase
    {
        private readonly IPecaRepository _pecaRepository;
        private readonly ILogger<CriarPecaUseCase> _logger;

        public CriarPecaUseCase(IPecaRepository pecaRepository, ILogger<CriarPecaUseCase> logger)
        {
            _pecaRepository = pecaRepository;
            _logger = logger;
        }

        public async Task<PecaResponse> HandleAsync(CriarPecaRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Criando peca: {Codigo}", request.Codigo);

            var existente = await _pecaRepository.GetByCodigoAsync(request.Codigo, cancellationToken);
            if (existente is not null)
                throw new ValidationException("Codigo", "Já existe uma peça com este código.");

            Peca peca;
            try
            {
                peca = new Peca(request.Codigo, request.Nome, request.PrecoUnitario, request.QuantidadeEstoque);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Peca", ex.Message);
            }

            var criada = await _pecaRepository.AddAsync(peca, cancellationToken);
            _logger.LogInformation("Peca criada com Id: {Id}", criada.Id);

            return MapToResponse(criada);
        }

        internal static PecaResponse MapToResponse(Peca p) => new()
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nome = p.Nome,
            PrecoUnitario = p.PrecoUnitario,
            QuantidadeEstoque = p.QuantidadeEstoque,
            CriadoEm = p.CreatedAt,
            AtualizadoEm = p.UpdatedAt
        };
    }
}
