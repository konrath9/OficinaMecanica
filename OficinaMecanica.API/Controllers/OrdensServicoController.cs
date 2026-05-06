using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.UseCases.OrdemServico;

namespace OficinaMecanica.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ordens-servico")]
    public class OrdensServicoController : ControllerBase
    {
        private readonly CriarOrdemServicoUseCase _criarOrdemServicoUseCase;
        private readonly ObterOrdemServicoUseCase _obterOrdemServicoUseCase;
        private readonly AdicionarServicoOrdemServicoUseCase _adicionarServicoUseCase;
        private readonly AdicionarPecaOrdemServicoUseCase _adicionarPecaUseCase;
        private readonly AlterarStatusOrdemServicoUseCase _alterarStatusUseCase;
        private readonly TempoMedioExecucaoUseCase _tempoMedioExecucaoUseCase;
        private readonly RegistrarExecucaoServicoUseCase _registrarExecucaoServicoUseCase;
        private readonly ILogger<OrdensServicoController> _logger;

        public OrdensServicoController(
            CriarOrdemServicoUseCase criarOrdemServicoUseCase,
            ObterOrdemServicoUseCase obterOrdemServicoUseCase,
            AdicionarServicoOrdemServicoUseCase adicionarServicoUseCase,
            AdicionarPecaOrdemServicoUseCase adicionarPecaUseCase,
            AlterarStatusOrdemServicoUseCase alterarStatusUseCase,
            TempoMedioExecucaoUseCase tempoMedioExecucaoUseCase,
            RegistrarExecucaoServicoUseCase registrarExecucaoServicoUseCase,
            ILogger<OrdensServicoController> logger)
        {
            _criarOrdemServicoUseCase = criarOrdemServicoUseCase;
            _obterOrdemServicoUseCase = obterOrdemServicoUseCase;
            _adicionarServicoUseCase = adicionarServicoUseCase;
            _adicionarPecaUseCase = adicionarPecaUseCase;
            _alterarStatusUseCase = alterarStatusUseCase;
            _tempoMedioExecucaoUseCase = tempoMedioExecucaoUseCase;
            _registrarExecucaoServicoUseCase = registrarExecucaoServicoUseCase;
            _logger = logger;
        }

        /// <summary>Cria uma nova Ordem de Serviço.</summary>
        [HttpPost]
        public async Task<IActionResult> CriarOrdemServico(
            [FromBody] CriarOrdemServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _criarOrdemServicoUseCase.HandleAsync(request, cancellationToken);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar OS");
                return StatusCode(500, new { message = "Erro ao criar a OS" });
            }
        }

        /// <summary>Lista todas as Ordens de Serviço.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterOrdemServicoUseCase.ListarAsync(cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar OS");
                return StatusCode(500, new { message = "Erro ao listar ordens de serviço" });
            }
        }

        /// <summary>Obtém uma Ordem de Serviço pelo Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterOrdemServicoUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter OS {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter a OS" });
            }
        }

        /// <summary>Adiciona um serviço a uma OS.</summary>
        [HttpPost("{id}/servicos")]
        public async Task<IActionResult> AdicionarServico(
            Guid id,
            [FromBody] AdicionarServicoOrdemServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.OrdemServicoId = id;
                var response = await _adicionarServicoUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar servico na OS {OrdemServicoId}", id);
                return StatusCode(500, new { message = "Erro ao adicionar serviço" });
            }
        }

        /// <summary>Adiciona uma peça a uma OS.</summary>
        [HttpPost("{id}/pecas")]
        public async Task<IActionResult> AdicionarPeca(
            Guid id,
            [FromBody] AdicionarPecaOrdemServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.OrdemServicoId = id;
                var response = await _adicionarPecaUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar peca na OS {OrdemServicoId}", id);
                return StatusCode(500, new { message = "Erro ao adicionar peça" });
            }
        }

        /// <summary>Altera o status de uma OS.</summary>
        /// <remarks>
        /// Envie o campo **acao** com um dos valores abaixo para avançar o fluxo da OS:
        ///
        /// | Valor | Ação | Status resultante | Status requerido |
        /// |-------|------|-------------------|-----------------|
        /// | 1 | IniciarDiagnostico | Em Diagnóstico | Recebida |
        /// | 2 | EnviarParaAprovacao | Aguardando Aprovação | Em Diagnóstico (requer ≥ 1 item) |
        /// | 3 | Aprovar | Em Execução | Aguardando Aprovação |
        /// | 4 | Finalizar | Finalizada | Em Execução (requer ≥ 1 item) |
        /// | 5 | Entregar | Entregue | Finalizada |
        /// | 6 | Cancelar | Cancelada | Qualquer (exceto Finalizada e Entregue) |
        ///
        /// Exemplo de body:
        ///
        ///     { "acao": 1 }
        ///
        /// Para cancelar com motivo:
        ///
        ///     { "acao": 6, "observacoes": "Cliente desistiu do serviço" }
        /// </remarks>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> AlterarStatus(
            Guid id,
            [FromBody] AlterarStatusOrdemServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.OrdemServicoId = id;
                var response = await _alterarStatusUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar status da OS {OrdemServicoId}", id);
                return StatusCode(500, new { message = "Erro ao alterar status" });
            }
        }

        /// <summary>Registra o início ou fim da execução de um serviço individual na OS.</summary>
        /// <remarks>
        /// Envie o campo **acao** com um dos valores:
        /// - `iniciar` — marca o início da execução do serviço (requer OS Em Execução)
        /// - `finalizar` — marca o fim da execução e registra a duração
        ///
        /// Exemplo:
        ///
        ///     { "acao": "iniciar" }
        /// </remarks>
        [HttpPut("{id}/servicos/{servicoId}/execucao")]
        public async Task<IActionResult> RegistrarExecucaoServico(
            Guid id,
            Guid servicoId,
            [FromBody] ExecucaoServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _registrarExecucaoServicoUseCase.HandleAsync(
                    new RegistrarExecucaoServicoRequest(id, servicoId, request.Acao),
                    cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar execucao do servico {ServicoId} na OS {OsId}", servicoId, id);
                return StatusCode(500, new { message = "Erro ao registrar execucao do servico" });
            }
        }

        /// <summary>
        /// Calcula o tempo médio de execução das Ordens de Serviço finalizadas.
        /// Aceita filtro opcional de período.
        /// </summary>
        [HttpGet("relatorios/tempo-medio-execucao")]
        public async Task<IActionResult> TempoMedioExecucao(
            [FromQuery] DateTime? periodoInicio,
            [FromQuery] DateTime? periodoFim,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _tempoMedioExecucaoUseCase.HandleAsync(periodoInicio, periodoFim, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular tempo medio de execucao");
                return StatusCode(500, new { message = "Erro ao calcular tempo médio de execução" });
            }
        }
    }
}
        