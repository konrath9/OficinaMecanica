using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Servicos
{
    public class AtualizarServicoUseCase
    {
        private readonly IServicoRepository _servicoRepository;
        private readonly ILogger<AtualizarServicoUseCase> _logger;

        public AtualizarServicoUseCase(IServicoRepository servicoRepository, ILogger<AtualizarServicoUseCase> logger)
        {
            _servicoRepository = servicoRepository;
            _logger = logger;
        }

        public async Task<ServicoResponse> HandleAsync(AtualizarServicoRequest request, CancellationToken cancellationToken = default)
        {
            var servico = await _servicoRepository.GetByIdAsync(request.Id, cancellationToken);
            if (servico is null)
            {
                _logger.LogWarning("Servico nao encontrado para atualizacao: {Id}", request.Id);
                throw new NotFoundException("Servico", request.Id);
            }

            var comMesmoNome = await _servicoRepository.GetByNomeAsync(request.Nome, cancellationToken);
            if (comMesmoNome is not null && comMesmoNome.Id != request.Id)
                throw new ValidationException("Nome", $"Já existe outro serviço com o nome '{request.Nome}'.");

            try
            {
                servico.Atualizar(request.Nome, request.Descricao, request.Preco);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Servico", ex.Message);
            }

            await _servicoRepository.UpdateAsync(servico, cancellationToken);
            _logger.LogInformation("Servico atualizado: {Id}", request.Id);

            return CriarServicoUseCase.MapToResponse(servico);
        }
    }
}
