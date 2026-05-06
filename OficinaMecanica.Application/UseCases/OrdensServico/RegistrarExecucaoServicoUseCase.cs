using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public record RegistrarExecucaoServicoRequest(Guid OrdemServicoId, Guid ServicoId, string Acao);

    public record RegistrarExecucaoServicoResponse(
        Guid OrdemServicoId,
        Guid ServicoId,
        string Descricao,
        DateTime? IniciadoEm,
        DateTime? FinalizadoEm,
        double? DuracaoHoras);

    public class RegistrarExecucaoServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly ILogger<RegistrarExecucaoServicoUseCase> _logger;

        public RegistrarExecucaoServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            ILogger<RegistrarExecucaoServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _logger = logger;
        }

        public async Task<RegistrarExecucaoServicoResponse> HandleAsync(
            RegistrarExecucaoServicoRequest request,
            CancellationToken cancellationToken = default)
        {
            var os = await _ordemServicoRepository.GetByIdAsync(request.OrdemServicoId, cancellationToken)
                ?? throw new NotFoundException("OrdemServico", request.OrdemServicoId);

            try
            {
                if (request.Acao.Equals("iniciar", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Iniciando servico {ServicoId} na OS {OsId}", request.ServicoId, request.OrdemServicoId);
                    os.IniciarExecucaoServico(request.ServicoId);
                }
                else if (request.Acao.Equals("finalizar", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Finalizando servico {ServicoId} na OS {OsId}", request.ServicoId, request.OrdemServicoId);
                    os.FinalizarExecucaoServico(request.ServicoId);
                }
                else
                {
                    throw new ValidationException("Acao", "Acao invalida. Use 'iniciar' ou 'finalizar'.");
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException("Execucao", ex.Message);
            }

            await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

            var item = os.Servicos.First(s => s.ServicoId == request.ServicoId);

            return new RegistrarExecucaoServicoResponse(
                OrdemServicoId: os.Id,
                ServicoId: item.ServicoId,
                Descricao: item.Descricao,
                IniciadoEm: item.IniciadoEm,
                FinalizadoEm: item.FinalizadoEm,
                DuracaoHoras: item.DuracaoHoras);
        }
    }
}
