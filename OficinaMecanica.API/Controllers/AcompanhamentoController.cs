using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.UseCases.OrdemServico;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Endpoint publico (sem autenticacao) para o cliente consultar o status da OS pelo numero
    /// e para receber notificacoes externas de aprovacao ou recusa do orcamento.
    /// </summary>
    [ApiController]
    [Route("api/acompanhamento")]
    public class AcompanhamentoController : ControllerBase
    {
        private readonly AcompanharOrdemServicoUseCase _acompanharUseCase;
        private readonly AprovarOrcamentoUseCase _aprovarOrcamentoUseCase;
        private readonly CancelarOrdemServicoUseCase _cancelarUseCase;
        private readonly ILogger<AcompanhamentoController> _logger;

        public AcompanhamentoController(
            AcompanharOrdemServicoUseCase acompanharUseCase,
            AprovarOrcamentoUseCase aprovarOrcamentoUseCase,
            CancelarOrdemServicoUseCase cancelarUseCase,
            ILogger<AcompanhamentoController> logger)
        {
            _acompanharUseCase = acompanharUseCase;
            _aprovarOrcamentoUseCase = aprovarOrcamentoUseCase;
            _cancelarUseCase = cancelarUseCase;
            _logger = logger;
        }

        /// <summary>Consulta o status de uma OS pelo n�mero.</summary>
        /// <param name="numero">N�mero da OS (ex: OS-2024-00001)</param>
        [HttpGet("{numero}")]
        public async Task<IActionResult> AcompanharPorNumero(string numero, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _acompanharUseCase.HandleAsync(numero, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar acompanhamento da OS {Numero}", numero);
                return StatusCode(500, new { message = "Erro ao consultar acompanhamento da OS" });
            }
        }

        /// <summary>Cliente aprova o or�amento � OS vai automaticamente para Em Execu��o.</summary>
        /// <remarks>
        /// A OS deve estar com status <b>AguardandoAprovacao</b>.
        /// Ap�s a aprova��o o status muda automaticamente para <b>EmExecucao</b>.
        /// </remarks>
        [HttpPost("{numero}/aprovar")]
        public async Task<IActionResult> AprovarOrcamento(string numero, CancellationToken cancellationToken)
        {
            try
            {
                var os = await _acompanharUseCase.HandleAsync(numero, cancellationToken);
                var response = await _aprovarOrcamentoUseCase.HandleAsync(os.OrdemServicoId, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aprovar orcamento da OS {Numero}", numero);
                return StatusCode(500, new { message = "Erro ao aprovar or�amento" });
            }
        }

        /// <summary>Cliente reprova o or�amento � OS � cancelada automaticamente.</summary>
        /// <remarks>
        /// A OS deve estar com status <b>AguardandoAprovacao</b>.
        /// Opcionalmente informe o motivo no corpo da requisi��o.
        ///
        /// Exemplo: <c>{ "motivo": "Valor acima do esperado" }</c>
        /// </remarks>
        [HttpPost("{numero}/reprovar")]
        public async Task<IActionResult> ReprovarOrcamento(
            string numero,
            [FromBody] ReprovarOrcamentoBody body,
            CancellationToken cancellationToken)
        {
            try
            {
                var os = await _acompanharUseCase.HandleAsync(numero, cancellationToken);
                var motivo = string.IsNullOrWhiteSpace(body.Motivo)
                    ? "Or�amento reprovado pelo cliente"
                    : $"Or�amento reprovado pelo cliente: {body.Motivo}";
                var response = await _cancelarUseCase.HandleAsync(
                    new CancelarOrdemServicoRequest(os.OrdemServicoId, motivo),
                    cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reprovar orcamento da OS {Numero}", numero);
                return StatusCode(500, new { message = "Erro ao reprovar or�amento" });
            }
        }
    }

    public record ReprovarOrcamentoBody(string? Motivo);
}
