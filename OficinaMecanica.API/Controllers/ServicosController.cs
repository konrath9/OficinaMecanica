using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Application.UseCases.Servicos;

namespace OficinaMecanica.API.Controllers
{
    [ApiController]
    [Route("api/servicos")]
    public class ServicosController : ControllerBase
    {
        private readonly CriarServicoUseCase _criarServicoUseCase;
        private readonly ObterServicoUseCase _obterServicoUseCase;
        private readonly AtualizarServicoUseCase _atualizarServicoUseCase;
        private readonly ExcluirServicoUseCase _excluirServicoUseCase;
        private readonly ILogger<ServicosController> _logger;

        public ServicosController(
            CriarServicoUseCase criarServicoUseCase,
            ObterServicoUseCase obterServicoUseCase,
            AtualizarServicoUseCase atualizarServicoUseCase,
            ExcluirServicoUseCase excluirServicoUseCase,
            ILogger<ServicosController> logger)
        {
            _criarServicoUseCase = criarServicoUseCase;
            _obterServicoUseCase = obterServicoUseCase;
            _atualizarServicoUseCase = atualizarServicoUseCase;
            _excluirServicoUseCase = excluirServicoUseCase;
            _logger = logger;
        }

        /// <summary>Lista todos os serviços do catálogo.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterServicoUseCase.ListarAsync(cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar servicos");
                return StatusCode(500, new { message = "Erro ao listar serviços" });
            }
        }

        /// <summary>Obtém um serviço pelo Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterServicoUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter servico {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter serviço" });
            }
        }

        /// <summary>Cria um novo serviço no catálogo.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _criarServicoUseCase.HandleAsync(request, cancellationToken);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar servico");
                return StatusCode(500, new { message = "Erro ao criar serviço" });
            }
        }

        /// <summary>Atualiza os dados de um serviço.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(
            Guid id,
            [FromBody] AtualizarServicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _atualizarServicoUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar servico {Id}", id);
                return StatusCode(500, new { message = "Erro ao atualizar serviço" });
            }
        }

        /// <summary>Exclui um serviço do catálogo.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _excluirServicoUseCase.HandleAsync(id, cancellationToken);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir servico {Id}", id);
                return StatusCode(500, new { message = "Erro ao excluir serviço" });
            }
        }
    }
}
