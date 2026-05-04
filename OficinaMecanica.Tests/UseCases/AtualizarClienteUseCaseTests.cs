using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.Clientes;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class AtualizarClienteUseCaseTests
    {
        private readonly Mock<IClienteRepository> _repoMock = new();
        private readonly Mock<ILogger<AtualizarClienteUseCase>> _loggerMock = new();

        private AtualizarClienteUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static Cliente ClienteValido() =>
            new("João Silva", "529.982.247-25", "joao@email.com", "11999999999");

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAtualizarCliente()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            var request = new AtualizarClienteRequest
            {
                Id = cliente.Id,
                Nome = "João Atualizado",
                Email = "novo@email.com",
                Telefone = "11988888888"
            };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal("João Atualizado", response.Nome);
            _repoMock.Verify(r => r.UpdateAsync(cliente, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Cliente?)null);

            var request = new AtualizarClienteRequest { Id = Guid.NewGuid(), Nome = "X" };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_ComDocumentoValido_DeveAtualizarDocumento()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.GetByDocumentoAsync("11144477735", default)).ReturnsAsync((Cliente?)null);

            var request = new AtualizarClienteRequest
            {
                Id = cliente.Id,
                Nome = "João",
                Documento = "111.444.777-35"
            };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal("11144477735", response.Documento);
        }

        [Fact]
        public async Task HandleAsync_DocumentoJaUsadoPorOutroCliente_DeveLancarValidationException()
        {
            var cliente = ClienteValido();
            var outro = new Cliente("Outro", "111.444.777-35");
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, CancellationToken.None)).ReturnsAsync(cliente);
            _repoMock.Setup(r => r.GetByDocumentoAsync("111.444.777-35", CancellationToken.None)).ReturnsAsync(outro);

            var request = new AtualizarClienteRequest
            {
                Id = cliente.Id,
                Nome = "João",
                Documento = "111.444.777-35"
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(request, CancellationToken.None));
            Assert.Contains("Documento", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_DocumentoInvalido_DeveLancarValidationException()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            var request = new AtualizarClienteRequest
            {
                Id = cliente.Id,
                Nome = "João",
                Documento = "111.111.111-11"
            };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_EmailInvalido_DeveLancarValidationException()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            var request = new AtualizarClienteRequest { Id = cliente.Id, Nome = "João", Email = "email-invalido" };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Email", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_TelefoneInvalido_DeveLancarValidationException()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            var request = new AtualizarClienteRequest { Id = cliente.Id, Nome = "João", Telefone = "123" };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Telefone", ex.Errors.Keys);
        }
    }
}
