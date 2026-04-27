using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Pecas;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.Pecas;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class CriarPecaUseCaseTests
    {
        private readonly Mock<IPecaRepository> _repoMock = new();
        private readonly Mock<ILogger<CriarPecaUseCase>> _loggerMock = new();

        private CriarPecaUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static Peca PecaValida() => new("OL-001", "Óleo 5W30", 45m, 10);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveCriarPeca()
        {
            _repoMock.Setup(r => r.GetByCodigoAsync("OL-001", default)).ReturnsAsync((Peca?)null);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Peca>(), default))
                     .ReturnsAsync((Peca p, CancellationToken _) => p);

            var request = new CriarPecaRequest { Codigo = "OL-001", Nome = "Óleo 5W30", PrecoUnitario = 45m, QuantidadeEstoque = 10 };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("OL-001", response.Codigo);
            Assert.Equal(10, response.QuantidadeEstoque);
        }

        [Fact]
        public async Task HandleAsync_CodigoDuplicado_DeveLancarValidationException()
        {
            _repoMock.Setup(r => r.GetByCodigoAsync("OL-001", default)).ReturnsAsync(PecaValida());

            var request = new CriarPecaRequest { Codigo = "OL-001", Nome = "Outro", PrecoUnitario = 10m };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Peca>(), default), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_DadosInvalidos_DeveLancarValidationException()
        {
            _repoMock.Setup(r => r.GetByCodigoAsync(It.IsAny<string>(), default)).ReturnsAsync((Peca?)null);

            var request = new CriarPecaRequest { Codigo = "OL-001", Nome = "", PrecoUnitario = 10m };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }
    }

    public class AtualizarPecaUseCaseTests
    {
        private readonly Mock<IPecaRepository> _repoMock = new();
        private readonly Mock<ILogger<AtualizarPecaUseCase>> _loggerMock = new();

        private AtualizarPecaUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static Peca PecaValida() => new("OL-001", "Óleo 5W30", 45m, 10);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAtualizarPeca()
        {
            var peca = PecaValida();
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new AtualizarPecaRequest { Id = peca.Id, Nome = "Óleo Sintético", PrecoUnitario = 80m };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal("Óleo Sintético", response.Nome);
            Assert.Equal(80m, response.PrecoUnitario);
            _repoMock.Verify(r => r.UpdateAsync(peca, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_PecaNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Peca?)null);

            var request = new AtualizarPecaRequest { Id = Guid.NewGuid(), Nome = "X", PrecoUnitario = 10m };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_ComCodigo_DeveAtualizarCodigo()
        {
            var peca = PecaValida();
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);
            _repoMock.Setup(r => r.GetByCodigoAsync("OL-002", default)).ReturnsAsync((Peca?)null);

            var request = new AtualizarPecaRequest { Id = peca.Id, Codigo = "OL-002", Nome = "Óleo 5W30", PrecoUnitario = 45m };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal("OL-002", response.Codigo);
        }

        [Fact]
        public async Task HandleAsync_CodigoDuplicadoDeOutraPeca_DeveLancarValidationException()
        {
            var peca = PecaValida();
            var outra = new Peca("OL-002", "Outra", 10m);
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);
            _repoMock.Setup(r => r.GetByCodigoAsync("OL-002", default)).ReturnsAsync(outra);

            var request = new AtualizarPecaRequest { Id = peca.Id, Codigo = "OL-002", Nome = "Óleo", PrecoUnitario = 45m };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }
    }

    public class ObterPecaUseCaseTests
    {
        private readonly Mock<IPecaRepository> _repoMock = new();
        private readonly Mock<ILogger<ObterPecaUseCase>> _loggerMock = new();

        private ObterPecaUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_PecaExistente_DeveRetornarResponse()
        {
            var peca = new Peca("OL-001", "Óleo", 45m, 5);
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var response = await CriarUseCase().HandleAsync(peca.Id);

            Assert.Equal(peca.Id, response.Id);
        }

        [Fact]
        public async Task HandleAsync_PecaNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Peca?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodasAsPecas()
        {
            var lista = new List<Peca> { new("OL-001", "Óleo", 45m, 5), new("FR-002", "Freio", 80m, 3) };
            _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(lista);

            var result = await CriarUseCase().ListarAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task ListarSemEstoqueAsync_DeveRetornarPecasSemEstoque()
        {
            var lista = new List<Peca> { new("OL-001", "Óleo", 45m, 0) };
            _repoMock.Setup(r => r.GetSemEstoqueAsync(default)).ReturnsAsync(lista);

            var result = await CriarUseCase().ListarSemEstoqueAsync();

            Assert.Single(result);
        }
    }

    public class ExcluirPecaUseCaseTests
    {
        private readonly Mock<IPecaRepository> _repoMock = new();
        private readonly Mock<ILogger<ExcluirPecaUseCase>> _loggerMock = new();

        private ExcluirPecaUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_PecaExistente_DeveExcluir()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.ExistsAsync(id, default)).ReturnsAsync(true);

            await CriarUseCase().HandleAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(id, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_PecaNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), default)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
        }
    }

    public class MovimentarEstoqueUseCaseTests
    {
        private readonly Mock<IPecaRepository> _repoMock = new();
        private readonly Mock<ILogger<MovimentarEstoqueUseCase>> _loggerMock = new();

        private MovimentarEstoqueUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        private static Peca PecaComEstoque(int qtd = 10) => new("OL-001", "Óleo", 45m, qtd);

        [Fact]
        public async Task HandleAsync_Entrada_DeveIncrementarEstoque()
        {
            var peca = PecaComEstoque(10);
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new MovimentarEstoqueRequest { Id = peca.Id, Tipo = "entrada", Quantidade = 5 };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal(15, response.QuantidadeEstoque);
            _repoMock.Verify(r => r.UpdateAsync(peca, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Saida_DeveDecrementarEstoque()
        {
            var peca = PecaComEstoque(10);
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new MovimentarEstoqueRequest { Id = peca.Id, Tipo = "saida", Quantidade = 3 };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal(7, response.QuantidadeEstoque);
        }

        [Fact]
        public async Task HandleAsync_TipoInvalido_DeveLancarValidationException()
        {
            var peca = PecaComEstoque();
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new MovimentarEstoqueRequest { Id = peca.Id, Tipo = "invalido", Quantidade = 5 };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Tipo", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_QuantidadeZero_DeveLancarValidationException()
        {
            var peca = PecaComEstoque();
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new MovimentarEstoqueRequest { Id = peca.Id, Tipo = "entrada", Quantidade = 0 };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_PecaNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Peca?)null);

            var request = new MovimentarEstoqueRequest { Id = Guid.NewGuid(), Tipo = "entrada", Quantidade = 5 };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_SaidaEstoqueInsuficiente_DeveLancarValidationException()
        {
            var peca = PecaComEstoque(2);
            _repoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new MovimentarEstoqueRequest { Id = peca.Id, Tipo = "saida", Quantidade = 10 };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Estoque", ex.Errors.Keys);
        }
    }
}
