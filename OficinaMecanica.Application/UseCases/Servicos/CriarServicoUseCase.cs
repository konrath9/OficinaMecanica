using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.Servicos
{
    public class CriarServicoUseCase
    {
        private readonly IServicoRepository _servicoRepository;
        private readonly ILogger<CriarServicoUseCase> _logger;

        public CriarServicoUseCase(IServicoRepository servicoRepository, ILogger<CriarServicoUseCase> logger)
        {
            _servicoRepository = servicoRepository;
            _logger = logger;
        }

        public async Task<ServicoResponse> HandleAsync(CriarServicoRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Criando servico: {Nome}", request.Nome);

            var existente = await _servicoRepository.GetByNomeAsync(request.Nome, cancellationToken);
            if (existente is not null)
                throw new ValidationException("Nome", $"Já existe um serviço com o nome '{request.Nome}'.");

            Servico servico;
            try
            {
                servico = new Servico(request.Nome, request.Descricao, request.Preco);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Servico", ex.Message);
            }

            var criado = await _servicoRepository.AddAsync(servico, cancellationToken);
            _logger.LogInformation("Servico criado com Id: {Id}", criado.Id);

            return MapToResponse(criado);
        }

        internal static ServicoResponse MapToResponse(Servico s) => new()
        {
            Id = s.Id,
            Nome = s.Nome,
            Descricao = s.Descricao,
            Preco = s.Preco,
            CriadoEm = s.CreatedAt,
            AtualizadoEm = s.UpdatedAt
        };
    }
}
