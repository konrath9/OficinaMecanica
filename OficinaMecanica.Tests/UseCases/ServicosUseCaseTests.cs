using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.Servicos;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.Servicos;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.UseCases
{
    public class CriarServicoUseCaseTests
    {
        private readonly Mock<IServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<CriarServicoUseCase>> _loggerMock = new();

        private CriarServicoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveCriarServico()
        {
            _repoMock.Setup(r => r.GetByNomeAsync("Troca de óleo", default)).ReturnsAsync((Servico?)null);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Servico>(), default))
                     .ReturnsAsync((Servico s, CancellationToken _) => s);

            var request = new CriarServicoRequest { Nome = "Troca de óleo", Descricao = "Troca de óleo do motor", Preco = 150m };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("Troca de óleo", response.Nome);
            Assert.Equal(150m, response.Preco);
        }

        [Fact]
        public async Task HandleAsync_NomeDuplicado_DeveLancarValidationException()
        {
            var existente = new Servico("Troca de óleo", "Desc", 150m);
            _repoMock.Setup(r => r.GetByNomeAsync("Troca de óleo", default)).ReturnsAsync(existente);

            var request = new CriarServicoRequest { Nome = "Troca de óleo", Descricao = "Desc", Preco = 100m };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Nome", ex.Errors.Keys);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Servico>(), default), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_DadosInvalidos_DeveLancarValidationException()
        {
            _repoMock.Setup(r => r.GetByNomeAsync(It.IsAny<string>(), default)).ReturnsAsync((Servico?)null);

            var request = new CriarServicoRequest { Nome = "X", Descricao = "", Preco = 100m };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_PrecoNegativo_DeveLancarValidationException()
        {
            _repoMock.Setup(r => r.GetByNomeAsync(It.IsAny<string>(), default)).ReturnsAsync((Servico?)null);

            var request = new CriarServicoRequest { Nome = "Serviço", Descricao = "Desc", Preco = -1m };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }
    }

    public class AtualizarServicoUseCaseTests
    {
        private readonly Mock<IServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<AtualizarServicoUseCase>> _loggerMock = new();

        private AtualizarServicoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAtualizarServico()
        {
            var servico = new Servico("Original", "Desc", 100m);
            _repoMock.Setup(r => r.GetByIdAsync(servico.Id, default)).ReturnsAsync(servico);
            _repoMock.Setup(r => r.GetByNomeAsync("Atualizado", default)).ReturnsAsync((Servico?)null);

            var request = new AtualizarServicoRequest { Id = servico.Id, Nome = "Atualizado", Descricao = "Nova desc", Preco = 200m };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal("Atualizado", response.Nome);
            Assert.Equal(200m, response.Preco);
            _repoMock.Verify(r => r.UpdateAsync(servico, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ServicoNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Servico?)null);

            var request = new AtualizarServicoRequest { Id = Guid.NewGuid(), Nome = "X", Descricao = "Y", Preco = 10m };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_NomeDuplicadoDeOutroServico_DeveLancarValidationException()
        {
            var servico = new Servico("Original", "Desc", 100m);
            var outro = new Servico("Duplicado", "Desc", 50m);
            _repoMock.Setup(r => r.GetByIdAsync(servico.Id, default)).ReturnsAsync(servico);
            _repoMock.Setup(r => r.GetByNomeAsync("Duplicado", default)).ReturnsAsync(outro);

            var request = new AtualizarServicoRequest { Id = servico.Id, Nome = "Duplicado", Descricao = "Desc", Preco = 100m };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Nome", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_MesmoNomeDoProprioServico_NaoDeveLancarExcecao()
        {
            var servico = new Servico("Original", "Desc", 100m);
            _repoMock.Setup(r => r.GetByIdAsync(servico.Id, default)).ReturnsAsync(servico);
            // retorna o próprio serviço (mesmo Id)
            _repoMock.Setup(r => r.GetByNomeAsync("Original", default)).ReturnsAsync(servico);

            var request = new AtualizarServicoRequest { Id = servico.Id, Nome = "Original", Descricao = "Nova desc", Preco = 120m };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal("Original", response.Nome);
        }
    }

    public class ObterServicoUseCaseTests
    {
        private readonly Mock<IServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<ObterServicoUseCase>> _loggerMock = new();

        private ObterServicoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ServicoExistente_DeveRetornarResponse()
        {
            var servico = new Servico("Troca de óleo", "Desc", 150m);
            _repoMock.Setup(r => r.GetByIdAsync(servico.Id, default)).ReturnsAsync(servico);

            var response = await CriarUseCase().HandleAsync(servico.Id);

            Assert.Equal(servico.Id, response.Id);
            Assert.Equal("Troca de óleo", response.Nome);
        }

        [Fact]
        public async Task HandleAsync_ServicoNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Servico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodosOsServicos()
        {
            var lista = new List<Servico>
            {
                new("Troca de óleo", "Desc", 150m),
                new("Alinhamento", "Desc", 80m)
            };
            _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(lista);

            var result = await CriarUseCase().ListarAsync();

            Assert.Equal(2, result.Count());
        }
    }

    public class ExcluirServicoUseCaseTests
    {
        private readonly Mock<IServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<ExcluirServicoUseCase>> _loggerMock = new();

        private ExcluirServicoUseCase CriarUseCase() => new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ServicoExistente_DeveExcluir()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.ExistsAsync(id, default)).ReturnsAsync(true);

            await CriarUseCase().HandleAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(id, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ServicoNaoEncontrado_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), default)).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
        }
    }
}
