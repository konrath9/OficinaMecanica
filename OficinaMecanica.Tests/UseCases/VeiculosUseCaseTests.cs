using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Veiculos;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.Veiculos;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class CriarVeiculoUseCaseTests
    {
        private readonly Mock<IVeiculoRepository> _veiculoRepoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<ILogger<CriarVeiculoUseCase>> _loggerMock = new();

        private CriarVeiculoUseCase CriarUseCase() =>
            new(_veiculoRepoMock.Object, _clienteRepoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveCriarVeiculo()
        {
            var clienteId = Guid.NewGuid();
            _clienteRepoMock.Setup(r => r.ExistsAsync(clienteId, CancellationToken.None)).ReturnsAsync(true);
            _veiculoRepoMock.Setup(r => r.GetByPlacaAsync("ABC1234", CancellationToken.None)).ReturnsAsync((Veiculo?)null);
            _veiculoRepoMock.Setup(r => r.AddAsync(It.IsAny<Veiculo>(), CancellationToken.None))
                            .ReturnsAsync((Veiculo v, CancellationToken _) => v);

            var request = new CriarVeiculoRequest { Placa = "ABC1234", Marca = "Toyota", Modelo = "Corolla", Ano = 2020, ClienteId = clienteId };

            var response = await CriarUseCase().HandleAsync(request, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("ABC1234", response.Placa);
        }

        [Fact]
        public async Task HandleAsync_ClienteNaoEncontrado_DeveLancarNotFoundException()
        {
            _clienteRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync(false);

            var request = new CriarVeiculoRequest { Placa = "ABC1234", Marca = "Toyota", Modelo = "Corolla", Ano = 2020, ClienteId = Guid.NewGuid() };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_PlacaDuplicada_DeveLancarValidationException()
        {
            var clienteId = Guid.NewGuid();
            _clienteRepoMock.Setup(r => r.ExistsAsync(clienteId, CancellationToken.None)).ReturnsAsync(true);
            _veiculoRepoMock.Setup(r => r.GetByPlacaAsync("ABC1234", CancellationToken.None))
                            .ReturnsAsync(new Veiculo("ABC1234", "Honda", "Civic", 2019, Guid.NewGuid()));

            var request = new CriarVeiculoRequest { Placa = "ABC1234", Marca = "Toyota", Modelo = "Corolla", Ano = 2020, ClienteId = clienteId };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request, CancellationToken.None));
            Assert.Contains("Placa", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_PlacaInvalida_DeveLancarValidationException()
        {
            var clienteId = Guid.NewGuid();
            _clienteRepoMock.Setup(r => r.ExistsAsync(clienteId, CancellationToken.None)).ReturnsAsync(true);
            _veiculoRepoMock.Setup(r => r.GetByPlacaAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync((Veiculo?)null);

            var request = new CriarVeiculoRequest { Placa = "INVALIDA", Marca = "Toyota", Modelo = "Corolla", Ano = 2020, ClienteId = clienteId };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request, CancellationToken.None));
            Assert.Contains("Placa", ex.Errors.Keys);
        }
    }

    public class AtualizarVeiculoUseCaseTests
    {
        private readonly Mock<IVeiculoRepository> _repoMock = new();
        private readonly Mock<ILogger<AtualizarVeiculoUseCase>> _loggerMock = new();

        private AtualizarVeiculoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAtualizar()
        {
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(veiculo.Id, CancellationToken.None)).ReturnsAsync(veiculo);

            var request = new AtualizarVeiculoRequest { Id = veiculo.Id, Marca = "Honda", Modelo = "Civic", Ano = 2022 };

            var response = await CriarUseCase().HandleAsync(request, CancellationToken.None);

            Assert.Equal("Honda", response.Marca);
            Assert.Equal("Civic", response.Modelo);
            _repoMock.Verify(r => r.UpdateAsync(veiculo, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_VeiculoNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((Veiculo?)null);

            var request = new AtualizarVeiculoRequest { Id = Guid.NewGuid(), Marca = "Honda", Modelo = "Civic", Ano = 2022 };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_AnoInvalido_DeveLancarValidationException()
        {
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(veiculo.Id, CancellationToken.None)).ReturnsAsync(veiculo);

            var request = new AtualizarVeiculoRequest { Id = veiculo.Id, Marca = "Honda", Modelo = "Civic", Ano = 1800 };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request, CancellationToken.None));
        }
    }

    public class ObterVeiculoUseCaseTests
    {
        private readonly Mock<IVeiculoRepository> _repoMock = new();
        private readonly Mock<ILogger<ObterVeiculoUseCase>> _loggerMock = new();

        private ObterVeiculoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_VeiculoExistente_DeveRetornarResponse()
        {
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(veiculo.Id, CancellationToken.None)).ReturnsAsync(veiculo);

            var response = await CriarUseCase().HandleAsync(veiculo.Id, CancellationToken.None);

            Assert.Equal(veiculo.Id, response.Id);
            Assert.Equal("Toyota", response.Marca);
        }

        [Fact]
        public async Task HandleAsync_VeiculoNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((Veiculo?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodosOsVeiculos()
        {
            var lista = new List<Veiculo>
            {
                new("ABC1234", "Toyota", "Corolla", 2020, Guid.NewGuid()),
                new("XYZ9999", "Honda", "Civic", 2021, Guid.NewGuid())
            };
            _repoMock.Setup(r => r.GetAllAsync(CancellationToken.None)).ReturnsAsync(lista);

            var result = await CriarUseCase().ListarAsync(CancellationToken.None);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task ListarPorClienteAsync_DeveRetornarVeiculosDoCliente()
        {
            var clienteId = Guid.NewGuid();
            var lista = new List<Veiculo> { new("ABC1234", "Toyota", "Corolla", 2020, clienteId) };
            _repoMock.Setup(r => r.GetByClienteIdAsync(clienteId, CancellationToken.None)).ReturnsAsync(lista);

            var result = await CriarUseCase().ListarPorClienteAsync(clienteId, CancellationToken.None);

            Assert.Single(result);
        }
    }

    public class ExcluirVeiculoUseCaseTests
    {
        private readonly Mock<IVeiculoRepository> _repoMock = new();
        private readonly Mock<ILogger<ExcluirVeiculoUseCase>> _loggerMock = new();

        private ExcluirVeiculoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_VeiculoExistente_DeveExcluir()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.ExistsAsync(id, CancellationToken.None)).ReturnsAsync(true);

            await CriarUseCase().HandleAsync(id, CancellationToken.None);

            _repoMock.Verify(r => r.DeleteAsync(id, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_VeiculoNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(Guid.NewGuid(), CancellationToken.None));
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
