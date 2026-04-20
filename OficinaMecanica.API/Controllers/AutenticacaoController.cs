using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Autenticacao;
using OficinaMecanica.Application.UseCases.Autenticacao;

namespace OficinaMecanica.API.Controllers
{
    /// <summary>
    /// Endpoint público de autenticação — não requer token.
    /// </summary>
    [ApiController]
    [Route("api/autenticacao")]
    public class AutenticacaoController : ControllerBase
    {
        private readonly LoginUseCase _loginUseCase;
        private readonly RegistrarUsuarioUseCase _registrarUsuarioUseCase;
        private readonly ILogger<AutenticacaoController> _logger;

        public AutenticacaoController(
            LoginUseCase loginUseCase,
            RegistrarUsuarioUseCase registrarUsuarioUseCase,
            ILogger<AutenticacaoController> logger)
        {
            _loginUseCase = loginUseCase;
            _registrarUsuarioUseCase = registrarUsuarioUseCase;
            _logger = logger;
        }

        /// <summary>Realiza login e retorna o token JWT.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _loginUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return Unauthorized(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar login");
                return StatusCode(500, new { message = "Erro ao realizar login" });
            }
        }

        /// <summary>
        /// Registra um novo usuário administrativo.
        /// Em produção este endpoint deve ser protegido por [Authorize(Roles = "Administrador")].
        /// </summary>
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar(
            [FromBody] RegistrarUsuarioRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var id = await _registrarUsuarioUseCase.HandleAsync(request, cancellationToken);
                return Created(string.Empty, new { id });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuário");
                return StatusCode(500, new { message = "Erro ao registrar usuário" });
            }
        }
    }
}
