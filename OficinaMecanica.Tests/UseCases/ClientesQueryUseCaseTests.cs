using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Clientes;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.Clientes;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class ObterClienteUseCaseTests
    {
        private readonly Mock<IClienteRepository> _repoMock = new();
        private readonly Mock<ILogger<ObterClienteUseCase>> _loggerMock = new();

        private ObterClienteUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static Cliente ClienteValido() => new("João Silva", "529.982.247-25", "joao@email.com");

        [Fact]
        public async Task HandleAsync_ClienteExistente_DeveRetornarResponse()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, CancellationToken.None)).ReturnsAsync(cliente);

            var response = await CriarUseCase().HandleAsync(cliente.Id, CancellationToken.None);

            Assert.Equal(cliente.Id, response.Id);
            Assert.Equal("João Silva", response.Nome);
        }

        [Fact]
        public async Task HandleAsync_ClienteNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((Cliente?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodosOsClientes()
        {
            var lista = new List<Cliente> { ClienteValido(), new("Maria", "111.444.777-35") };
            _repoMock.Setup(r => r.GetAllAsync(CancellationToken.None)).ReturnsAsync(lista);

            var result = await CriarUseCase().ListarAsync(CancellationToken.None);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task BuscarPorDocumentoAsync_DocumentoVazio_DeveLancarValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().BuscarPorDocumentoAsync("", CancellationToken.None));
        }

        [Fact]
        public async Task BuscarPorDocumentoAsync_ClienteEncontrado_DeveRetornarResponse()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByDocumentoAsync("529.982.247-25", CancellationToken.None)).ReturnsAsync(cliente);

            var response = await CriarUseCase().BuscarPorDocumentoAsync("529.982.247-25", CancellationToken.None);

            Assert.Equal(cliente.Id, response.Id);
        }

        [Fact]
        public async Task BuscarPorDocumentoAsync_ClienteNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByDocumentoAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync((Cliente?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().BuscarPorDocumentoAsync("529.982.247-25", CancellationToken.None));
        }
    }

    public class ExcluirClienteUseCaseTests
    {
        private readonly Mock<IClienteRepository> _repoMock = new();
        private readonly Mock<ILogger<ExcluirClienteUseCase>> _loggerMock = new();

        private ExcluirClienteUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ClienteExistente_DeveExcluir()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.ExistsAsync(id, CancellationToken.None)).ReturnsAsync(true);

            await CriarUseCase().HandleAsync(id, CancellationToken.None);

            _repoMock.Verify(r => r.DeleteAsync(id, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(Guid.NewGuid(), CancellationToken.None));
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
