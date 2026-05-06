using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    public record RegistrarExecucaoServicoRequest(Guid OrdemServicoId, Guid ServicoId, string Acao);

    public record RegistrarExecucaoServicoResponse(
        Guid OrdemServicoId,
        Guid ServicoId,
        string Descricao,
        DateTime? IniciadoEm,
        DateTime? FinalizadoEm,
        double? DuracaoHoras,
        bool OsFinalizadaAutomaticamente);

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

            // Automatiza: se todos os servicos forem finalizados, finaliza a OS automaticamente
            var osFinalizadaAutomaticamente = false;
            if (request.Acao.Equals("finalizar", StringComparison.OrdinalIgnoreCase))
            {
                var todosFinalizados = os.Servicos.Any() && os.Servicos.All(s => s.FinalizadoEm.HasValue);
                if (todosFinalizados)
                {
                    try
                    {
                        os.Finalizar();
                        osFinalizadaAutomaticamente = true;
                        await _ordemServicoRepository.UpdateAsync(os, cancellationToken);
                        _logger.LogInformation("OS {OsId} finalizada automaticamente pois todos os servicos foram concluidos", os.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        // OS pode nao estar em EmExecucao - nao finaliza automaticamente
                    }
                }
            }

            var item = os.Servicos.First(s => s.ServicoId == request.ServicoId);

            return new RegistrarExecucaoServicoResponse(
                OrdemServicoId: os.Id,
                ServicoId: item.ServicoId,
                Descricao: item.Descricao,
                IniciadoEm: item.IniciadoEm,
                FinalizadoEm: item.FinalizadoEm,
                DuracaoHoras: item.DuracaoHoras,
                OsFinalizadaAutomaticamente: osFinalizadaAutomaticamente);
        }
    }
}
