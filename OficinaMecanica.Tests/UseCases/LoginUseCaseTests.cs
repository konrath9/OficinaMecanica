using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Autenticacao;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Application.UseCases.Autenticacao;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Tests.UseCases
{
    public class LoginUseCaseTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepositoryMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ISenhaService> _senhaServiceMock = new();
        private readonly Mock<ILogger<LoginUseCase>> _loggerMock = new();

        private LoginUseCase CriarUseCase() => new(
            _usuarioRepositoryMock.Object,
            _tokenServiceMock.Object,
            _senhaServiceMock.Object,
            _loggerMock.Object);

        private static Usuario CriarUsuarioAtivo() =>
            new("Admin", "admin@oficina.com", "hash_bcrypt", PerfilUsuario.Administrador);

        [Fact]
        public async Task HandleAsync_CredenciaisValidas_DeveRetornarToken()
        {
            // Arrange
            var usuario = CriarUsuarioAtivo();

            _usuarioRepositoryMock
                .Setup(r => r.GetByEmailAsync("admin@oficina.com", default))
                .ReturnsAsync(usuario);

            _senhaServiceMock
                .Setup(s => s.Verificar("senha123", "hash_bcrypt"))
                .Returns(true);

            _tokenServiceMock
                .Setup(t => t.GerarToken(usuario))
                .Returns("jwt.token.gerado");

            var request = new LoginRequest { Email = "admin@oficina.com", Senha = "senha123" };

            // Act
            var response = await CriarUseCase().HandleAsync(request);

            // Assert
            Assert.Equal("jwt.token.gerado", response.Token);
            Assert.Equal("Admin", response.NomeUsuario);
            Assert.Equal("Administrador", response.Perfil);
        }

        [Fact]
        public async Task HandleAsync_EmailNaoEncontrado_DeveLancarValidationException()
        {
            // Arrange
            _usuarioRepositoryMock
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
                .ReturnsAsync((Usuario?)null);

            var request = new LoginRequest { Email = "inexistente@email.com", Senha = "senha123" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(request));

            Assert.Contains("Credenciais", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_SenhaIncorreta_DeveLancarValidationException()
        {
            // Arrange
            var usuario = CriarUsuarioAtivo();

            _usuarioRepositoryMock
                .Setup(r => r.GetByEmailAsync("admin@oficina.com", default))
                .ReturnsAsync(usuario);

            _senhaServiceMock
                .Setup(s => s.Verificar(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            var request = new LoginRequest { Email = "admin@oficina.com", Senha = "senha_errada" };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(request));

            _tokenServiceMock.Verify(t => t.GerarToken(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_UsuarioInativo_DeveLancarValidationException()
        {
            // Arrange
            var usuario = CriarUsuarioAtivo();
            usuario.Desativar();

            _usuarioRepositoryMock
                .Setup(r => r.GetByEmailAsync("admin@oficina.com", default))
                .ReturnsAsync(usuario);

            var request = new LoginRequest { Email = "admin@oficina.com", Senha = "senha123" };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(request));
        }

        [Theory]
        [InlineData("", "senha123")]
        [InlineData("admin@email.com", "")]
        public async Task HandleAsync_CamposObrigatoriosVazios_DeveLancarValidationException(
            string email, string senha)
        {
            var request = new LoginRequest { Email = email, Senha = senha };

            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(request));
        }
    }
}
