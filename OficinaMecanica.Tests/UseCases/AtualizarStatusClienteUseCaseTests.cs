using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.Clientes;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class AtualizarStatusClienteUseCaseTests
    {
        private readonly Mock<IClienteRepository> _repoMock = new();
        private readonly Mock<ILogger<AtualizarStatusClienteUseCase>> _loggerMock = new();

        private AtualizarStatusClienteUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static Cliente ClienteValido() =>
            new("Jo�o Silva", "529.982.247-25", "joao@email.com", "11999999999");

        [Fact]
        public async Task HandleAsync_DesativarClienteAtivo_DeveDesativar()
        {
            var cliente = ClienteValido();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            var response = await CriarUseCase().HandleAsync(cliente.Id, ativo: false);

            Assert.False(response.Ativo);
            _repoMock.Verify(r => r.UpdateAsync(cliente, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_AtivarClienteInativo_DeveAtivar()
        {
            var cliente = ClienteValido();
            cliente.Desativar();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            var response = await CriarUseCase().HandleAsync(cliente.Id, ativo: true);

            Assert.True(response.Ativo);
        }

        [Fact]
        public async Task HandleAsync_ClienteNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Cliente?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid(), ativo: false));
        }

        [Fact]
        public async Task HandleAsync_DesativarClienteJaInativo_DeveLancarValidationException()
        {
            var cliente = ClienteValido();
            cliente.Desativar();
            _repoMock.Setup(r => r.GetByIdAsync(cliente.Id, default)).ReturnsAsync(cliente);

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(cliente.Id, ativo: false));
        }
    }
}
