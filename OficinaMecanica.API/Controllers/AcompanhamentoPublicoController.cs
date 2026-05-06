using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.UseCases.OrdemServico;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Controller público — não requer autenticação.
    /// Permite que o cliente final acompanhe o status da sua OS pelo número.
    /// </summary>
    [ApiController]
    [Route("api/publico/acompanhamento")]
    public class AcompanhamentoPublicoController : ControllerBase
    {
        private readonly AcompanharOrdemServicoUseCase _acompanharOrdemServicoUseCase;
        private readonly ILogger<AcompanhamentoPublicoController> _logger;

        public AcompanhamentoPublicoController(
            AcompanharOrdemServicoUseCase acompanharOrdemServicoUseCase,
            ILogger<AcompanhamentoPublicoController> logger)
        {
            _acompanharOrdemServicoUseCase = acompanharOrdemServicoUseCase;
            _logger = logger;
        }

        /// <summary>
        /// Consulta pública do status de uma Ordem de Serviço pelo número.
        /// Não expõe dados sensíveis do cliente.
        /// </summary>
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
    }
}
