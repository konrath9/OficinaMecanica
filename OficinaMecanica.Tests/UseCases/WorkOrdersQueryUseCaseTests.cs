using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.WorkOrders;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.UseCases
{
    public class ObterOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IVeiculoRepository> _veiculoRepoMock = new();
        private readonly Mock<ILogger<ObterOrdemServicoUseCase>> _loggerMock = new();

        private ObterOrdemServicoUseCase CriarUseCase() =>
            new(_osRepoMock.Object, _clienteRepoMock.Object, _veiculoRepoMock.Object, _loggerMock.Object);

        private static OrdemServico OsValida()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m, 1));
            return os;
        }

        [Fact]
        public async Task HandleAsync_OSExistente_DeveRetornarResponse()
        {
            var os = OsValida();
            var cliente = new Cliente("João", "529.982.247-25");
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, os.ClienteId);

            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, CancellationToken.None)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, CancellationToken.None)).ReturnsAsync(cliente);
            _veiculoRepoMock.Setup(r => r.GetByIdAsync(os.VeiculoId, CancellationToken.None)).ReturnsAsync(veiculo);

            var response = await CriarUseCase().HandleAsync(os.Id, CancellationToken.None);

            Assert.Equal(os.Id, response.Id);
            Assert.Equal("OS-001", response.Numero);
            Assert.Equal("João", response.NomeCliente);
        }

        [Fact]
        public async Task HandleAsync_SemClienteNemVeiculo_DeveRetornarComValoresVazios()
        {
            var os = OsValida();
            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, CancellationToken.None)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, CancellationToken.None)).ReturnsAsync((Cliente?)null);
            _veiculoRepoMock.Setup(r => r.GetByIdAsync(os.VeiculoId, CancellationToken.None)).ReturnsAsync((Veiculo?)null);

            var response = await CriarUseCase().HandleAsync(os.Id, CancellationToken.None);

            Assert.Equal(string.Empty, response.NomeCliente);
            Assert.Equal(string.Empty, response.DescricaoVeiculo);
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _osRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodasAsOrdens()
        {
            var lista = new List<OrdemServico> { OsValida(), OsValida() };
            _osRepoMock.Setup(r => r.GetAllAsync(CancellationToken.None)).ReturnsAsync(lista);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((Cliente?)null);
            _veiculoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((Veiculo?)null);

            var result = await CriarUseCase().ListarAsync(CancellationToken.None);

            Assert.Equal(2, result.Count());
        }
    }

    public class TempoMedioExecucaoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<TempoMedioExecucaoUseCase>> _loggerMock = new();

        private TempoMedioExecucaoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static OrdemServico OsFinalizadaComTempo(double horas)
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Serviço", 100m, 1));
            os.IniciarDiagnostico();
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.Finalizar();

            // Ajusta as datas via reflexão para simular tempo de execução
            var tipo = typeof(OrdemServico);
            tipo.GetProperty("IniciadaEm")!
                .SetValue(os, DateTime.UtcNow.AddHours(-horas));
            tipo.GetProperty("FinalizadaEm")!
                .SetValue(os, DateTime.UtcNow);
            return os;
        }

        [Fact]
        public async Task HandleAsync_ComOrdens_DeveCalcularTempoMedio()
        {
            var lista = new List<OrdemServico>
            {
                OsFinalizadaComTempo(2),
                OsFinalizadaComTempo(4)
            };
            _repoMock.Setup(r => r.GetFinalizadasNoPeriodoAsync(null, null, CancellationToken.None)).ReturnsAsync(lista);

            var response = await CriarUseCase().HandleAsync(cancellationToken: CancellationToken.None);

            Assert.Equal(2, response.TotalOrdensFinalizadas);
            Assert.True(response.TempoMedioHoras > 0);
        }

        [Fact]
        public async Task HandleAsync_SemOrdens_DeveRetornarTempoZero()
        {
            _repoMock.Setup(r => r.GetFinalizadasNoPeriodoAsync(null, null, CancellationToken.None))
                     .ReturnsAsync(new List<OrdemServico>());

            var response = await CriarUseCase().HandleAsync(cancellationToken: CancellationToken.None);

            Assert.Equal(0, response.TempoMedioHoras);
            Assert.Equal(0, response.TotalOrdensFinalizadas);
        }

        [Fact]
        public async Task HandleAsync_ComPeriodo_DevePassarFiltro()
        {
            var inicio = DateTime.UtcNow.AddDays(-30);
            var fim = DateTime.UtcNow;
            _repoMock.Setup(r => r.GetFinalizadasNoPeriodoAsync(inicio, fim, CancellationToken.None))
                     .ReturnsAsync(new List<OrdemServico>());

            var response = await CriarUseCase().HandleAsync(inicio, fim, CancellationToken.None);

            Assert.Equal(inicio, response.PeriodoInicio);
            Assert.Equal(fim, response.PeriodoFim);
            _repoMock.Verify(r => r.GetFinalizadasNoPeriodoAsync(inicio, fim, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OrdensSemdatas_DeveIgnorar()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid()); // sem IniciadaEm nem FinalizadaEm
            _repoMock.Setup(r => r.GetFinalizadasNoPeriodoAsync(null, null, CancellationToken.None))
                     .ReturnsAsync(new List<OrdemServico> { os });

            var response = await CriarUseCase().HandleAsync(cancellationToken: CancellationToken.None);

            Assert.Equal(0, response.TotalOrdensFinalizadas);
        }
    }

    public class AcompanharOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepoMock = new();
        private readonly Mock<IVeiculoRepository> _veiculoRepoMock = new();
        private readonly Mock<ILogger<AcompanharOrdemServicoUseCase>> _loggerMock = new();

        private AcompanharOrdemServicoUseCase CriarUseCase() =>
            new(_osRepoMock.Object, _veiculoRepoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_OSExistente_DeveRetornarAcompanhamento()
        {
            var os = new OrdemServico("OS-2024-001", Guid.NewGuid(), Guid.NewGuid());
            var veiculo = new Veiculo("ABC1234", "Toyota", "Corolla", 2020, os.ClienteId);
            _osRepoMock.Setup(r => r.GetByNumeroAsync("OS-2024-001", CancellationToken.None)).ReturnsAsync(os);
            _veiculoRepoMock.Setup(r => r.GetByIdAsync(os.VeiculoId, CancellationToken.None)).ReturnsAsync(veiculo);

            var response = await CriarUseCase().HandleAsync("OS-2024-001", CancellationToken.None);

            Assert.Equal("OS-2024-001", response.Numero);
            Assert.Contains("Toyota", response.DescricaoVeiculo);
        }

        [Fact]
        public async Task HandleAsync_SemVeiculo_DeveRetornarDescricaoVazia()
        {
            var os = new OrdemServico("OS-2024-001", Guid.NewGuid(), Guid.NewGuid());
            _osRepoMock.Setup(r => r.GetByNumeroAsync("OS-2024-001", CancellationToken.None)).ReturnsAsync(os);
            _veiculoRepoMock.Setup(r => r.GetByIdAsync(os.VeiculoId, CancellationToken.None)).ReturnsAsync((Veiculo?)null);

            var response = await CriarUseCase().HandleAsync("OS-2024-001", CancellationToken.None);

            Assert.Equal(string.Empty, response.DescricaoVeiculo);
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _osRepoMock.Setup(r => r.GetByNumeroAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync("OS-INEXISTENTE", CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_NumeroVazio_DeveLancarValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync("", CancellationToken.None));
        }
    }
}
