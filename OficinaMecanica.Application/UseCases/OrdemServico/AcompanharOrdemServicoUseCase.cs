using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Application.UseCases.OrdemServico
{
    /// <summary>
    /// Consulta a OS pelo numero. A partir da Fase 3, o controller exige autenticacao via CPF
    /// (Function Serverless) e restringe o acesso ao proprio cliente dono da OS - a checagem de
    /// posse fica no controller, que ja recebe o ClienteId aqui no response pra comparar com o token.
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
                throw new ValidationException("Numero", "N�mero da OS � obrigat�rio.");

            _logger.LogInformation("Acompanhamento p�blico da OS: {Numero}", numero);

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
                ClienteId = os.ClienteId,
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
            Domain.Enums.StatusOrdemServico.Recebida => "Sua OS foi recebida e est� na fila de atendimento.",
            Domain.Enums.StatusOrdemServico.EmDiagnostico => "O t�cnico est� realizando o diagn�stico do ve�culo.",
            Domain.Enums.StatusOrdemServico.AguardandoAprovacao => "O or�amento foi enviado. Aguardando sua aprova��o para iniciar os servi�os.",
            Domain.Enums.StatusOrdemServico.EmExecucao => "Os servi�os est�o sendo executados.",
            Domain.Enums.StatusOrdemServico.Finalizada => "Todos os servi�os foram conclu�dos. Seu ve�culo est� pronto para retirada.",
            Domain.Enums.StatusOrdemServico.Entregue => "Ve�culo entregue. Obrigado pela prefer�ncia!",
            Domain.Enums.StatusOrdemServico.Cancelada => "Esta OS foi cancelada.",
            _ => string.Empty
        };
    }
}
