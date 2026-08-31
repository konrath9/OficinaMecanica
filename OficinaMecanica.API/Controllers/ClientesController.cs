using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.UseCases.Clientes;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Gestao de clientes. Restrito a staff (Administrador/Mecanico/Recepcionista) - um token de
    /// Cliente (emitido pela Function Serverless via CPF, Fase 3) nao pode listar, alterar ou
    /// excluir cadastros de clientes, apenas consultar/agir sobre a propria OS via AcompanhamentoController.
    /// </summary>
    [Authorize(Roles = "Administrador,Mecanico,Recepcionista")]
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        private readonly CriarClienteUseCase _criarClienteUseCase;
        private readonly ObterClienteUseCase _obterClienteUseCase;
        private readonly AtualizarClienteUseCase _atualizarClienteUseCase;
        private readonly AtualizarStatusClienteUseCase _atualizarStatusClienteUseCase;
        private readonly ExcluirClienteUseCase _excluirClienteUseCase;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            CriarClienteUseCase criarClienteUseCase,
            ObterClienteUseCase obterClienteUseCase,
            AtualizarClienteUseCase atualizarClienteUseCase,
            AtualizarStatusClienteUseCase atualizarStatusClienteUseCase,
            ExcluirClienteUseCase excluirClienteUseCase,
            ILogger<ClientesController> logger)
        {
            _criarClienteUseCase = criarClienteUseCase;
            _obterClienteUseCase = obterClienteUseCase;
            _atualizarClienteUseCase = atualizarClienteUseCase;
            _atualizarStatusClienteUseCase = atualizarStatusClienteUseCase;
            _excluirClienteUseCase = excluirClienteUseCase;
            _logger = logger;
        }

        /// <summary>Lista todos os clientes.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterClienteUseCase.ListarAsync(cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar clientes");
                return StatusCode(500, new { message = "Erro ao listar clientes" });
            }
        }

        /// <summary>Obt�m um cliente pelo Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterClienteUseCase.HandleAsync(id, cancellationToken);
                return Ok(response);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cliente {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter cliente" });
            }
        }

        /// <summary>Busca um cliente pelo CPF ou CNPJ. Usado para identificar o cliente ao abrir uma OS.</summary>
        [HttpGet("por-documento/{documento}")]
        public async Task<IActionResult> BuscarPorDocumento(string documento, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _obterClienteUseCase.BuscarPorDocumentoAsync(documento, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cliente pelo documento {Documento}", documento);
                return StatusCode(500, new { message = "Erro ao buscar cliente pelo documento" });
            }
        }

        /// <summary>Cria um novo cliente.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromBody] CriarClienteRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _criarClienteUseCase.HandleAsync(request, cancellationToken);
                return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cliente");
                return StatusCode(500, new { message = "Erro ao criar cliente" });
            }
        }

        /// <summary>Atualiza os dados de um cliente.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(
            Guid id,
            [FromBody] AtualizarClienteRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _atualizarClienteUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cliente {Id}", id);
                return StatusCode(500, new { message = "Erro ao atualizar cliente" });
            }
        }

        /// <summary>Ativa ou desativa um cliente.</summary>
        /// <remarks>
        /// Clientes inativos nao conseguem autenticar via CPF (Function Serverless da Fase 3).
        /// Exemplo: <c>{ "ativo": false }</c>
        /// </remarks>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> AtualizarStatus(
            Guid id,
            [FromBody] AtualizarStatusClienteRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _atualizarStatusClienteUseCase.HandleAsync(id, request.Ativo, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors }); }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status do cliente {Id}", id);
                return StatusCode(500, new { message = "Erro ao atualizar status do cliente" });
            }
        }

        /// <summary>Exclui um cliente.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _excluirClienteUseCase.HandleAsync(id, cancellationToken);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir cliente {Id}", id);
                return StatusCode(500, new { message = "Erro ao excluir cliente" });
            }
        }
    }
}
