using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.UseCases.OrdemServico;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Permite que o cliente acompanhe o status da OS e aprove o orçamento.
    /// Requer autenticação JWT — o cliente cria uma conta e faz login normalmente.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/acompanhamento")]
    public class AcompanhamentoController : ControllerBase
    {
        private readonly AcompanharOrdemServicoUseCase _acompanharUseCase;
        private readonly AprovarOrcamentoUseCase _aprovarOrcamentoUseCase;
        private readonly ILogger<AcompanhamentoController> _logger;

        public AcompanhamentoController(
            AcompanharOrdemServicoUseCase acompanharUseCase,
            AprovarOrcamentoUseCase aprovarOrcamentoUseCase,
            ILogger<AcompanhamentoController> logger)
        {
            _acompanharUseCase = acompanharUseCase;
            _aprovarOrcamentoUseCase = aprovarOrcamentoUseCase;
            _logger = logger;
        }

        /// <summary>Consulta o status de uma OS pelo número.</summary>
        /// <param name="numero">Número da OS (ex: OS-2024-00001)</param>
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

        /// <summary>Cliente aprova o orçamento — OS vai automaticamente para Em Execução.</summary>
        /// <remarks>
        /// A OS deve estar com status <b>AguardandoAprovacao</b>.
        /// Após a aprovação o status muda automaticamente para <b>EmExecucao</b>.
        /// </remarks>
        /// <param name="numero">Número da OS</param>
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
                return StatusCode(500, new { message = "Erro ao aprovar orçamento" });
            }
        }
    }
}
