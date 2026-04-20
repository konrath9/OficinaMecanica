using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Autenticacao;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.Autenticacao
{
    public class RegistrarUsuarioUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ISenhaService _senhaService;
        private readonly ILogger<RegistrarUsuarioUseCase> _logger;

        public RegistrarUsuarioUseCase(
            IUsuarioRepository usuarioRepository,
            ISenhaService senhaService,
            ILogger<RegistrarUsuarioUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _senhaService = senhaService;
            _logger = logger;
        }

        public async Task<Guid> HandleAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Registrando usuário: {Email}", request.Email);

            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.Nome))
                errors["Nome"] = new[] { "Nome é obrigatório." };

            if (string.IsNullOrWhiteSpace(request.Email))
                errors["Email"] = new[] { "E-mail é obrigatório." };

            if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 6)
                errors["Senha"] = new[] { "Senha deve ter pelo menos 6 caracteres." };

            if (errors.Any())
                throw new ValidationException(errors);

            var emailNormalizado = request.Email.Trim().ToLowerInvariant();

            var emailEmUso = await _usuarioRepository.ExisteEmailAsync(emailNormalizado, cancellationToken);
            if (emailEmUso)
            {
                _logger.LogWarning("Registro falhou — e-mail já em uso: {Email}", emailNormalizado);
                throw new ValidationException("Email", "Este e-mail já está em uso.");
            }

            var senhaHash = _senhaService.Criptografar(request.Senha);

            Usuario usuario;
            try
            {
                usuario = new Usuario(request.Nome, emailNormalizado, senhaHash, request.Perfil);
            }
            catch (ArgumentException ex)
            {
                throw new ValidationException("Usuario", ex.Message);
            }

            var criado = await _usuarioRepository.AddAsync(usuario, cancellationToken);
            _logger.LogInformation("Usuário registrado com Id: {Id}", criado.Id);

            return criado.Id;
        }
    }
}
