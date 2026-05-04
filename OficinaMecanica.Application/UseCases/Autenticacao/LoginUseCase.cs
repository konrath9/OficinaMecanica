using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Autenticacao;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;

namespace OficinaMecanica.Application.UseCases.Autenticacao
{
    public class LoginUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenService _tokenService;
        private readonly ISenhaService _senhaService;
        private readonly ILogger<LoginUseCase> _logger;

        public LoginUseCase(
            IUsuarioRepository usuarioRepository,
            ITokenService tokenService,
            ISenhaService senhaService,
            ILogger<LoginUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _tokenService = tokenService;
            _senhaService = senhaService;
            _logger = logger;
        }

        public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
                throw new ValidationException("Credenciais", "E-mail e senha são obrigatórios.");

            _logger.LogInformation("Tentativa de login: {Email}", request.Email);

            var usuario = await _usuarioRepository.GetByEmailAsync(
                request.Email.Trim().ToLowerInvariant(), cancellationToken);

            if (usuario is null || !usuario.Ativo)
            {
                _logger.LogWarning("Login falhou — usuário não encontrado ou inativo: {Email}", request.Email);
                throw new ValidationException("Credenciais", "E-mail ou senha inválidos.");
            }

            if (!_senhaService.Verificar(request.Senha, usuario.SenhaHash))
            {
                _logger.LogWarning("Login falhou — senha incorreta: {Email}", request.Email);
                throw new ValidationException("Credenciais", "E-mail ou senha inválidos.");
            }

            var token = _tokenService.GerarToken(usuario);

            _logger.LogInformation("Login realizado com sucesso: {Email}", request.Email);

            return new LoginResponse
            {
                Token = token,
                NomeUsuario = usuario.Nome,
                Email = usuario.Email,
                Perfil = usuario.Perfil.ToString(),
                ExpiraEm = DateTime.UtcNow.AddHours(24)
            };
        }
    }
}
