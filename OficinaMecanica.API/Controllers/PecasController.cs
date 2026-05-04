using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.UseCases.Pecas;

namespace OficinaMecanica.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/pecas")]
    public class PecasController : ControllerBase
    {
        private readonly CriarPecaUseCase _criarPecaUseCase;
        private readonly ObterPecaUseCase _obterPecaUseCase;
        private readonly AtualizarPecaUseCase _atualizarPecaUseCase;
        private readonly ExcluirPecaUseCase _excluirPecaUseCase;
        private readonly MovimentarEstoqueUseCase _movimentarEstoqueUseCase;
        private readonly ILogger<PecasController> _logger;

        public PecasController(
            CriarPecaUseCase criarPecaUseCase,
            ObterPecaUseCase obterPecaUseCase,
            AtualizarPecaUseCase atualizarPecaUseCase,
            ExcluirPecaUseCase excluirPecaUseCase,
            MovimentarEstoqueUseCase movimentarEstoqueUseCase,
            ILogger<PecasController> logger)
        {
            _criarPecaUseCase = criarPecaUseCase;
            _obterPecaUseCase = obterPecaUseCase;
            _atualizarPecaUseCase = atualizarPecaUseCase;
            _excluirPecaUseCase = excluirPecaUseCase;
            _movimentarEstoqueUseCase = movimentarEstoqueUseCase;
            _logger = logger;
        }

        /// <summary>Lista todas as peças do estoque.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterPecaUseCase.ListarAsync(cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar pecas");
                return StatusCode(500, new { message = "Erro ao listar peças" });
            }
        }

        /// <summary>Lista peças com estoque zerado.</summary>
        [HttpGet("sem-estoque")]
        public async Task<IActionResult> ListarSemEstoque(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterPecaUseCase.ListarSemEstoqueAsync(cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar pecas sem estoque");
                return StatusCode(500, new { message = "Erro ao listar peças sem estoque" });
            }
        }

        /// <summary>Obtém uma peça pelo Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterPecaUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter peca {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter peça" });
            }
        }

        /// <summary>Cria uma nova peça no estoque.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarPecaRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _criarPecaUseCase.HandleAsync(request, cancellationToken);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar peca");
                return StatusCode(500, new { message = "Erro ao criar peça" });
            }
        }

        /// <summary>Atualiza os dados de uma peça.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(
            Guid id,
            [FromBody] AtualizarPecaRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _atualizarPecaUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar peca {Id}", id);
                return StatusCode(500, new { message = "Erro ao atualizar peça" });
            }
        }

        /// <summary>Exclui uma peça do estoque.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _excluirPecaUseCase.HandleAsync(id, cancellationToken);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir peca {Id}", id);
                return StatusCode(500, new { message = "Erro ao excluir peça" });
            }
        }

        /// <summary>
        /// Movimenta o estoque de uma peça.
        /// Informe <c>tipo</c> como <b>"entrada"</b> (incrementa) ou <b>"saida"</b> (decrementa) e a <c>quantidade</c>.
        /// </summary>
        [HttpPatch("{id}/estoque")]
        public async Task<IActionResult> MovimentarEstoque(
            Guid id,
            [FromBody] MovimentarEstoqueRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _movimentarEstoqueUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao movimentar estoque da peca {Id}", id);
                return StatusCode(500, new { message = "Erro ao movimentar estoque" });
            }
        }
    }
}
