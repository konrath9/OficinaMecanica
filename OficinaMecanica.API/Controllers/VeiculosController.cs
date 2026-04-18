using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Veiculos;
using OficinaMecanica.Application.UseCases.Veiculos;

namespace OficinaMecanica.API.Controllers
{
    [ApiController]
    [Route("api/veiculos")]
    public class VeiculosController : ControllerBase
    {
        private readonly CriarVeiculoUseCase _criarVeiculoUseCase;
        private readonly ObterVeiculoUseCase _obterVeiculoUseCase;
        private readonly AtualizarVeiculoUseCase _atualizarVeiculoUseCase;
        private readonly ExcluirVeiculoUseCase _excluirVeiculoUseCase;
        private readonly ILogger<VeiculosController> _logger;

        public VeiculosController(
            CriarVeiculoUseCase criarVeiculoUseCase,
            ObterVeiculoUseCase obterVeiculoUseCase,
            AtualizarVeiculoUseCase atualizarVeiculoUseCase,
            ExcluirVeiculoUseCase excluirVeiculoUseCase,
            ILogger<VeiculosController> logger)
        {
            _criarVeiculoUseCase = criarVeiculoUseCase;
            _obterVeiculoUseCase = obterVeiculoUseCase;
            _atualizarVeiculoUseCase = atualizarVeiculoUseCase;
            _excluirVeiculoUseCase = excluirVeiculoUseCase;
            _logger = logger;
        }

        /// <summary>Lista todos os veículos.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterVeiculoUseCase.ListarAsync(cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar veiculos");
                return StatusCode(500, new { message = "Erro ao listar veículos" });
            }
        }

        /// <summary>Obtém um veículo pelo Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterVeiculoUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter veiculo {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter veículo" });
            }
        }

        /// <summary>Lista todos os veículos de um cliente.</summary>
        [HttpGet("por-cliente/{clienteId}")]
        public async Task<IActionResult> ListarPorCliente(Guid clienteId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterVeiculoUseCase.ListarPorClienteAsync(clienteId, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar veiculos do cliente {ClienteId}", clienteId);
                return StatusCode(500, new { message = "Erro ao listar veículos do cliente" });
            }
        }

        /// <summary>Cria um novo veículo.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarVeiculoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _criarVeiculoUseCase.HandleAsync(request, cancellationToken);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar veiculo");
                return StatusCode(500, new { message = "Erro ao criar veículo" });
            }
        }

        /// <summary>Atualiza os dados de um veículo.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(
            Guid id,
            [FromBody] AtualizarVeiculoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _atualizarVeiculoUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar veiculo {Id}", id);
                return StatusCode(500, new { message = "Erro ao atualizar veículo" });
            }
        }

        /// <summary>Exclui um veículo.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _excluirVeiculoUseCase.HandleAsync(id, cancellationToken);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir veiculo {Id}", id);
                return StatusCode(500, new { message = "Erro ao excluir veículo" });
            }
        }
    }
}
