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
        private readonly RegistrarExecucaoServicoUseCase _registrarExecucaoServicoUseCase;
        private readonly ConcluirDiagnosticoUseCase _concluirDiagnosticoUseCase;
        private readonly RegistrarEntregaUseCase _registrarEntregaUseCase;
        private readonly CancelarOrdemServicoUseCase _cancelarUseCase;
        private readonly TempoMedioExecucaoUseCase _tempoMedioExecucaoUseCase;
        private readonly ILogger<OrdensServicoController> _logger;

        public OrdensServicoController(
            CriarOrdemServicoUseCase criarOrdemServicoUseCase,
            ObterOrdemServicoUseCase obterOrdemServicoUseCase,
            AdicionarServicoOrdemServicoUseCase adicionarServicoUseCase,
            AdicionarPecaOrdemServicoUseCase adicionarPecaUseCase,
            RegistrarExecucaoServicoUseCase registrarExecucaoServicoUseCase,
            ConcluirDiagnosticoUseCase concluirDiagnosticoUseCase,
            RegistrarEntregaUseCase registrarEntregaUseCase,
            CancelarOrdemServicoUseCase cancelarUseCase,
            TempoMedioExecucaoUseCase tempoMedioExecucaoUseCase,
            ILogger<OrdensServicoController> logger)
        {
            _criarOrdemServicoUseCase = criarOrdemServicoUseCase;
            _obterOrdemServicoUseCase = obterOrdemServicoUseCase;
            _adicionarServicoUseCase = adicionarServicoUseCase;
            _adicionarPecaUseCase = adicionarPecaUseCase;
            _registrarExecucaoServicoUseCase = registrarExecucaoServicoUseCase;
            _concluirDiagnosticoUseCase = concluirDiagnosticoUseCase;
            _registrarEntregaUseCase = registrarEntregaUseCase;
            _cancelarUseCase = cancelarUseCase;
            _tempoMedioExecucaoUseCase = tempoMedioExecucaoUseCase;
            _logger = logger;
        }

        /// <summary>Cria uma nova Ordem de Serviço.</summary>
        /// <remarks>
        /// Ao criar, a OS recebe automaticamente o status <b>Recebida</b>.
        /// O status avança para <b>EmDiagnostico</b> assim que o primeiro serviço ou peça for adicionado.
        /// </remarks>
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

        /// <summary>Adiciona um serviço à OS.</summary>
        /// <remarks>
        /// Se a OS estiver em <b>Recebida</b>, o status avança automaticamente para <b>EmDiagnostico</b>.
        /// </remarks>
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

        /// <summary>Adiciona uma peça à OS.</summary>
        /// <remarks>
        /// Se a OS estiver em <b>Recebida</b>, o status avança automaticamente para <b>EmDiagnostico</b>.
        /// Desconta a quantidade do estoque da peça automaticamente.
        /// </remarks>
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

        /// <summary>Técnico conclui o diagnóstico e envia o orçamento para aprovação do cliente.</summary>
        /// <remarks>
        /// O sistema move automaticamente o status para <b>AguardandoAprovacao</b>.
        /// Requer que a OS esteja em <b>EmDiagnostico</b> e possua ao menos um serviço ou peça.
        /// O cliente poderá aprovar via: <c>POST /api/publico/acompanhamento/{numero}/aprovar</c>
        /// </remarks>
        [HttpPost("{id}/concluir-diagnostico")]
        public async Task<IActionResult> ConcluirDiagnostico(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _concluirDiagnosticoUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao concluir diagnostico da OS {OsId}", id);
                return StatusCode(500, new { message = "Erro ao concluir diagnóstico" });
            }
        }

        /// <summary>Registra o início ou fim da execução de um serviço individual na OS.</summary>
        /// <remarks>
        /// Envie o campo <b>acao</b> com um dos valores:
        /// - <c>iniciar</c> — marca o início (requer OS em <b>EmExecucao</b>)
        /// - <c>finalizar</c> — marca o fim e registra a duração. Se for o último serviço, a OS avança automaticamente para <b>Finalizada</b>
        ///
        /// Exemplo: <c>{ "acao": "iniciar" }</c>
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
                return StatusCode(500, new { message = "Erro ao registrar execução do serviço" });
            }
        }

        /// <summary>Registra a entrega do veículo ao cliente.</summary>
        /// <remarks>
        /// O sistema move automaticamente o status para <b>Entregue</b> e registra o timestamp da entrega.
        /// Requer que a OS esteja em <b>Finalizada</b>.
        /// </remarks>
        [HttpPost("{id}/registrar-entrega")]
        public async Task<IActionResult> RegistrarEntrega(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _registrarEntregaUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar entrega da OS {OsId}", id);
                return StatusCode(500, new { message = "Erro ao registrar entrega" });
            }
        }

        /// <summary>Cancela uma Ordem de Serviço.</summary>
        /// <remarks>
        /// Permitido em qualquer status, exceto <b>Finalizada</b> e <b>Entregue</b>.
        ///
        /// Exemplo: <c>{ "motivo": "Cliente desistiu do serviço" }</c>
        /// </remarks>
        [HttpPost("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(
            Guid id,
            [FromBody] CancelarOrdemServicoBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _cancelarUseCase.HandleAsync(
                    new CancelarOrdemServicoRequest(id, body.Motivo),
                    cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar OS {OsId}", id);
                return StatusCode(500, new { message = "Erro ao cancelar a OS" });
            }
        }

        /// <summary>Calcula o tempo médio de execução das OS finalizadas. Aceita filtro de período.</summary>
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

    public record CancelarOrdemServicoBody(string? Motivo);
}
