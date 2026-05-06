using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Enums;
using OficinaMecanica.Application.UseCases.OrdemServico;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Controller público — não requer autenticação.
    /// Permite que o cliente final acompanhe o status da sua OS e aprove o orçamento.
    /// </summary>
    [ApiController]
    [Route("api/publico/acompanhamento")]
    public class AcompanhamentoPublicoController : ControllerBase
    {
        private readonly AcompanharOrdemServicoUseCase _acompanharOrdemServicoUseCase;
        private readonly AlterarStatusOrdemServicoUseCase _alterarStatusUseCase;
        private readonly ILogger<AcompanhamentoPublicoController> _logger;

        public AcompanhamentoPublicoController(
            AcompanharOrdemServicoUseCase acompanharOrdemServicoUseCase,
            AlterarStatusOrdemServicoUseCase alterarStatusUseCase,
            ILogger<AcompanhamentoPublicoController> logger)
        {
            _acompanharOrdemServicoUseCase = acompanharOrdemServicoUseCase;
            _alterarStatusUseCase = alterarStatusUseCase;
            _logger = logger;
        }

        /// <summary>Consulta pública do status de uma OS pelo número. Não expõe dados sensíveis.</summary>
        /// <param name="numero">Número da OS (ex: OS-2024-00001)</param>
        [HttpGet("{numero}")]
        public async Task<IActionResult> AcompanharPorNumero(string numero, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _acompanharOrdemServicoUseCase.HandleAsync(numero, cancellationToken);
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

        /// <summary>Aprovação do orçamento pelo cliente — não requer autenticação.</summary>
        /// <remarks>
        /// A OS deve estar com status **AguardandoAprovacao** para que a aprovação seja aceita.
        /// Após aprovação o status muda automaticamente para **EmExecucao**.
        /// </remarks>
        /// <param name="numero">Número da OS</param>
        [HttpPost("{numero}/aprovar")]
        public async Task<IActionResult> AprovarOrcamento(string numero, CancellationToken cancellationToken)
        {
            try
            {
                var os = await _acompanharOrdemServicoUseCase.HandleAsync(numero, cancellationToken);
                var request = new AlterarStatusOrdemServicoRequest
                {
                    OrdemServicoId = os.OrdemServicoId,
                    Acao = AcaoOrdemServico.Aprovar
                };
                var response = await _alterarStatusUseCase.HandleAsync(request, cancellationToken);
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
