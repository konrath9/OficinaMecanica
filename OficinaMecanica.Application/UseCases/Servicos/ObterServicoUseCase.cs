using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.Servicos
{
    public class ObterServicoUseCase
    {
        private readonly IServicoRepository _servicoRepository;
        private readonly ILogger<ObterServicoUseCase> _logger;

        public ObterServicoUseCase(IServicoRepository servicoRepository, ILogger<ObterServicoUseCase> logger)
        {
            _servicoRepository = servicoRepository;
            _logger = logger;
        }

        public async Task<ServicoResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var servico = await _servicoRepository.GetByIdAsync(id, cancellationToken);
            if (servico is null)
            {
                _logger.LogWarning("Servico nao encontrado: {Id}", id);
                throw new NotFoundException("Servico", id);
            }

            return CriarServicoUseCase.MapToResponse(servico);
        }

        public async Task<IEnumerable<ServicoResponse>> ListarAsync(CancellationToken cancellationToken = default)
        {
            var servicos = await _servicoRepository.GetAllAsync(cancellationToken);
            return servicos.Select(CriarServicoUseCase.MapToResponse);
        }
    }
}
