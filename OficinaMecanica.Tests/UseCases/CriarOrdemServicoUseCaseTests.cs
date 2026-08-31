using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class CriarOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepositoryMock = new();
        private readonly Mock<IClienteRepository> _clienteRepositoryMock = new();
        private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock = new();
        private readonly Mock<IGeradorNumeroOrdemServico> _geradorNumeroMock = new();
        private readonly OrdemServicoMetrics _metrics = new();
        private readonly Mock<ILogger<CriarOrdemServicoUseCase>> _loggerMock = new();

        private CriarOrdemServicoUseCase CriarUseCase() => new(
            _osRepositoryMock.Object,
            _clienteRepositoryMock.Object,
            _veiculoRepositoryMock.Object,
            _geradorNumeroMock.Object,
            _metrics,
            _loggerMock.Object);

        private static readonly Guid ClienteId = Guid.NewGuid();
        private static readonly Guid VeiculoId = Guid.NewGuid();

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveCriarOrdemServico()
        {
            // Arrange
            _clienteRepositoryMock
                .Setup(r => r.ExistsAsync(ClienteId, default))
                .ReturnsAsync(true);

            _veiculoRepositoryMock
                .Setup(r => r.ExistsAsync(VeiculoId, default))
                .ReturnsAsync(true);

            _geradorNumeroMock
                .Setup(g => g.GerarAsync(default))
                .ReturnsAsync("OS-20240101-1234");

            _osRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<OrdemServico>(), default))
                .ReturnsAsync((OrdemServico os, CancellationToken _) => os);

            var request = new CriarOrdemServicoRequest
            {
                ClienteId = ClienteId,
                VeiculoId = VeiculoId,
                Observacoes = "Troca de �leo"
            };

            // Act
            var response = await CriarUseCase().HandleAsync(request);

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("OS-20240101-1234", response.Numero);
            _osRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OrdemServico>(), default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteInexistente_DeveLancarNotFoundException()
        {
            // Arrange
            _clienteRepositoryMock
                .Setup(r => r.ExistsAsync(ClienteId, default))
                .ReturnsAsync(false);

            var request = new CriarOrdemServicoRequest
            {
                ClienteId = ClienteId,
                VeiculoId = VeiculoId
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(request));

            _osRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_VeiculoInexistente_DeveLancarNotFoundException()
        {
            // Arrange
            _clienteRepositoryMock
                .Setup(r => r.ExistsAsync(ClienteId, default))
                .ReturnsAsync(true);

            _veiculoRepositoryMock
                .Setup(r => r.ExistsAsync(VeiculoId, default))
                .ReturnsAsync(false);

            var request = new CriarOrdemServicoRequest
            {
                ClienteId = ClienteId,
                VeiculoId = VeiculoId
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(request));

            _osRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ClienteIdVazio_DeveLancarValidationException()
        {
            // Arrange � ambos vazios para cair no caminho de acumula��o de erros
            var request = new CriarOrdemServicoRequest
            {
                ClienteId = Guid.Empty,
                VeiculoId = Guid.Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(request));
        }
    }
}
