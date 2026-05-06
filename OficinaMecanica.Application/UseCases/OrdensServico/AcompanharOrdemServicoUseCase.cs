using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    /// <summary>
    /// Endpoint público: qualquer pessoa pode acompanhar o status da OS pelo número, sem autenticação.
    /// Retorna apenas dados não sensíveis.
    /// </summary>
    public class AcompanharOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ILogger<AcompanharOrdemServicoUseCase> _logger;

        public AcompanharOrdemServicoUseCase(
            IOrdemServicoRepository ordemServicoRepository,
            IVeiculoRepository veiculoRepository,
            ILogger<AcompanharOrdemServicoUseCase> logger)
        {
            _ordemServicoRepository = ordemServicoRepository;
            _veiculoRepository = veiculoRepository;
            _logger = logger;
        }

        public async Task<AcompanhamentoOrdemServicoResponse> HandleAsync(string numero, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ValidationException("Numero", "Número da OS é obrigatório.");

            _logger.LogInformation("Acompanhamento público da OS: {Numero}", numero);

            var os = await _ordemServicoRepository.GetByNumeroAsync(numero, cancellationToken);
            if (os is null)
            {
                _logger.LogWarning("OS nao encontrada para acompanhamento: {Numero}", numero);
                throw new NotFoundException("OrdemServico", numero);
            }

            var veiculo = await _veiculoRepository.GetByIdAsync(os.VeiculoId, cancellationToken);

            return new AcompanhamentoOrdemServicoResponse
            {
                OrdemServicoId = os.Id,
                Numero = os.Numero,
                Status = os.Status,
                StatusDescricao = os.Status.ToString(),
                MensagemStatus = ObterMensagemStatus(os.Status),
                DescricaoVeiculo = veiculo is not null ? $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano} - {veiculo.Placa}" : string.Empty,
                Observacoes = os.Observacoes,
                TotalOrcamento = os.TotalOrcamento,
                CriadaEm = os.CreatedAt,
                IniciadaEm = os.IniciadaEm,
                FinalizadaEm = os.FinalizadaEm,
                EntregueEm = os.EntregueEm,
                Progresso = new ProgressoServicosResponse
                {
                    Concluidos = os.ProgressoServicos.Concluidos,
                    Total = os.ProgressoServicos.Total
                },
                Historico = os.HistoricoStatus
                    .OrderBy(h => h.OcorridoEm)
                    .Select(h => new HistoricoStatusResponse
                    {
                        Status = h.Status.ToString(),
                        OcorridoEm = h.OcorridoEm,
                        Observacao = h.Observacao
                    })
                    .ToList()
            };
        }

        private static string ObterMensagemStatus(Domain.Enums.StatusOrdemServico status) => status switch
        {
            Domain.Enums.StatusOrdemServico.Recebida => "Sua OS foi recebida e está na fila de atendimento.",
            Domain.Enums.StatusOrdemServico.EmDiagnostico => "O técnico está realizando o diagnóstico do veículo.",
            Domain.Enums.StatusOrdemServico.AguardandoAprovacao => "O orçamento foi enviado. Aguardando sua aprovação para iniciar os serviços.",
            Domain.Enums.StatusOrdemServico.EmExecucao => "Os serviços estão sendo executados.",
            Domain.Enums.StatusOrdemServico.Finalizada => "Todos os serviços foram concluídos. Seu veículo está pronto para retirada.",
            Domain.Enums.StatusOrdemServico.Entregue => "Veículo entregue. Obrigado pela preferência!",
            Domain.Enums.StatusOrdemServico.Cancelada => "Esta OS foi cancelada.",
            _ => string.Empty
        };
    }
}
