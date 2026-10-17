using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Infrastructure.Mensageria;
using OficinaMecanica.Infrastructure.Persistence;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Gestao operacional das OS. Restrito a staff (Administrador/Mecanico/Recepcionista) - o cliente
    /// acompanha e aprova/reprova sua propria OS via AcompanhamentoController, com token de Cliente.
    /// A execucao (fila, diagnostico e reparo) fica no Execution Service; orcamento e pagamento no Billing Service.
    /// </summary>
    [Authorize(Roles = "Administrador,Mecanico,Recepcionista")]
    [ApiController]
    [Route("api/ordens-servico")]
    public class OrdensServicoController : ControllerBase
    {
        private readonly CriarOrdemServicoUseCase _criarOrdemServicoUseCase;
        private readonly ObterOrdemServicoUseCase _obterOrdemServicoUseCase;
        private readonly AdicionarServicoOrdemServicoUseCase _adicionarServicoUseCase;
        private readonly AdicionarPecaOrdemServicoUseCase _adicionarPecaUseCase;
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

        /// <summary>Lista as Ordens de Serviço em andamento, priorizadas por status.</summary>
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

        /// <summary>Estado atual da Saga distribuída da OS (orquestrador MassTransit).</summary>
        /// <remarks>
        /// Mostra em que passo a OS está na coordenação entre OS, Billing e Execution Service
        /// (ex.: <c>AguardandoPagamento</c>, <c>EmExecucao</c>, <c>Estornando</c>, <c>Cancelada</c>).
        /// </remarks>
        [HttpGet("{id}/saga")]
        public async Task<IActionResult> ObterSaga(Guid id, [FromServices] OficinaMecanicaDbContext db, CancellationToken cancellationToken)
        {
            var saga = await db.Set<OrdemServicoSagaState>().AsNoTracking()
                .FirstOrDefaultAsync(s => s.CorrelationId == id, cancellationToken);

            return saga is null
                ? NotFound(new { message = "Saga não iniciada para esta OS (o diagnóstico ainda não foi concluído)." })
                : Ok(saga);
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

        /// <summary>Técnico conclui o diagnóstico - inicia a Saga da OS.</summary>
        /// <remarks>
        /// Requer que a OS esteja em <b>EmDiagnostico</b> e possua ao menos um serviço ou peça.
        /// O orçamento é gerado de forma assíncrona pelo <b>Billing Service</b>; quando ele confirma, a OS vai
        /// para <b>AguardandoAprovacao</b> e o cliente pode aprovar via <c>POST /api/acompanhamento/{numero}/aprovar</c>.
        /// </remarks>
        [HttpPost("{id}/concluir-diagnostico")]
        public async Task<IActionResult> ConcluirDiagnostico(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _concluirDiagnosticoUseCase.HandleAsync(id, cancellationToken);
                return Accepted(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao concluir diagnostico da OS {OsId}", id);
                return StatusCode(500, new { message = "Erro ao concluir diagnóstico" });
            }
        }

        /// <summary>Registra a entrega do veículo ao cliente.</summary>
        /// <remarks>
        /// Move o status para <b>Entregue</b> e registra o timestamp da entrega. Requer a OS em <b>Finalizada</b>.
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
        /// Permitido em qualquer status, exceto <b>Finalizada</b> e <b>Entregue</b>. A Saga compensa nos outros
        /// serviços o que já tiver acontecido (cancela o orçamento, estorna o pagamento, tira a OS da fila de execução).
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

        /// <summary>Tempo médio de execução (EmExecucao → Finalizada) das OS finalizadas. Aceita filtro de período.</summary>
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
