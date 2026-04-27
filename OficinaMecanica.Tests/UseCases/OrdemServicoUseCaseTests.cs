using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Enums;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.WorkOrders;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.UseCases
{
    public class AlterarStatusOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<AlterarStatusOrdemServicoUseCase>> _loggerMock = new();

        private AlterarStatusOrdemServicoUseCase CriarUseCase() =>
            new(_repoMock.Object, _loggerMock.Object);

        private static OrdemServico OsComServico()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m, 1));
            return os;
        }

        [Fact]
        public async Task HandleAsync_IniciarDiagnostico_DeveAlterarStatus()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var request = new AlterarStatusOrdemServicoRequest { OrdemServicoId = os.Id, Acao = AcaoOrdemServico.IniciarDiagnostico };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal(os.Id, response.OrdemServicoId);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_FluxoCompleto_DevePercorrerTodosOsStatus()
        {
            var os = OsComServico();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var acoes = new[]
            {
                AcaoOrdemServico.IniciarDiagnostico,
                AcaoOrdemServico.EnviarParaAprovacao,
                AcaoOrdemServico.Aprovar,
                AcaoOrdemServico.Finalizar,
                AcaoOrdemServico.Entregar
            };

            foreach (var acao in acoes)
            {
                var req = new AlterarStatusOrdemServicoRequest { OrdemServicoId = os.Id, Acao = acao };
                await CriarUseCase().HandleAsync(req);
            }

            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Exactly(5));
        }

        [Fact]
        public async Task HandleAsync_Cancelar_DeveAlterarParaCancelada()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var request = new AlterarStatusOrdemServicoRequest { OrdemServicoId = os.Id, Acao = AcaoOrdemServico.Cancelar };

            await CriarUseCase().HandleAsync(request);

            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            var request = new AlterarStatusOrdemServicoRequest { OrdemServicoId = Guid.NewGuid(), Acao = AcaoOrdemServico.IniciarDiagnostico };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_IdVazio_DeveLancarValidationException()
        {
            var request = new AlterarStatusOrdemServicoRequest { OrdemServicoId = Guid.Empty, Acao = AcaoOrdemServico.IniciarDiagnostico };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_TransicaoInvalida_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            // Status=Recebida, tentar Entregar diretamente é inválido
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var request = new AlterarStatusOrdemServicoRequest { OrdemServicoId = os.Id, Acao = AcaoOrdemServico.Entregar };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_RequestNulo_DeveLancarArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CriarUseCase().HandleAsync(null!));
        }
    }

    public class AdicionarServicoOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepoMock = new();
        private readonly Mock<IServicoRepository> _servicoRepoMock = new();
        private readonly Mock<ILogger<AdicionarServicoOrdemServicoUseCase>> _loggerMock = new();

        private AdicionarServicoOrdemServicoUseCase CriarUseCase() =>
            new(_osRepoMock.Object, _servicoRepoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAdicionarServico()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            var servico = new Servico("Troca de óleo", "Desc", 150m);

            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _servicoRepoMock.Setup(r => r.GetByIdAsync(servico.Id, default)).ReturnsAsync(servico);

            var request = new AdicionarServicoOrdemServicoRequest
            {
                OrdemServicoId = os.Id,
                ServicoId = servico.Id,
                Quantidade = 1
            };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal(os.Id, response.OrdemServicoId);
            _osRepoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _osRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            var request = new AdicionarServicoOrdemServicoRequest
            {
                OrdemServicoId = Guid.NewGuid(),
                ServicoId = Guid.NewGuid(),
                Quantidade = 1
            };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_ServicoNaoEncontrado_DeveLancarNotFoundException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _servicoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Servico?)null);

            var request = new AdicionarServicoOrdemServicoRequest
            {
                OrdemServicoId = os.Id,
                ServicoId = Guid.NewGuid(),
                Quantidade = 1
            };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_IdsVazios_DeveLancarValidationException()
        {
            var request = new AdicionarServicoOrdemServicoRequest
            {
                OrdemServicoId = Guid.Empty,
                ServicoId = Guid.Empty,
                Quantidade = 1
            };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_QuantidadeZero_DeveLancarValidationException()
        {
            var request = new AdicionarServicoOrdemServicoRequest
            {
                OrdemServicoId = Guid.NewGuid(),
                ServicoId = Guid.NewGuid(),
                Quantidade = 0
            };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }
    }

    public class AdicionarPecaOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepoMock = new();
        private readonly Mock<IPecaRepository> _pecaRepoMock = new();
        private readonly Mock<ILogger<AdicionarPecaOrdemServicoUseCase>> _loggerMock = new();

        private AdicionarPecaOrdemServicoUseCase CriarUseCase() =>
            new(_osRepoMock.Object, _pecaRepoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAdicionarPecaEBaixarEstoque()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            var peca = new Peca("OL-001", "Óleo 5W30", 45m, 10);

            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _pecaRepoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new AdicionarPecaOrdemServicoRequest
            {
                OrdemServicoId = os.Id,
                PecaId = peca.Id,
                Quantidade = 2
            };

            var response = await CriarUseCase().HandleAsync(request);

            Assert.Equal(os.Id, response.OrdemServicoId);
            Assert.Equal(8, peca.QuantidadeEstoque); // 10 - 2
            _pecaRepoMock.Verify(r => r.UpdateAsync(peca, default), Times.Once);
            _osRepoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_EstoqueInsuficiente_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            var peca = new Peca("OL-001", "Óleo", 45m, 1);

            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _pecaRepoMock.Setup(r => r.GetByIdAsync(peca.Id, default)).ReturnsAsync(peca);

            var request = new AdicionarPecaOrdemServicoRequest
            {
                OrdemServicoId = os.Id,
                PecaId = peca.Id,
                Quantidade = 5
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
            Assert.Contains("Peca", ex.Errors.Keys);
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _osRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            var request = new AdicionarPecaOrdemServicoRequest
            {
                OrdemServicoId = Guid.NewGuid(),
                PecaId = Guid.NewGuid(),
                Quantidade = 1
            };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_PecaNaoEncontrada_DeveLancarNotFoundException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _osRepoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _pecaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Peca?)null);

            var request = new AdicionarPecaOrdemServicoRequest
            {
                OrdemServicoId = os.Id,
                PecaId = Guid.NewGuid(),
                Quantidade = 1
            };

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(request));
        }

        [Fact]
        public async Task HandleAsync_IdsVazios_DeveLancarValidationException()
        {
            var request = new AdicionarPecaOrdemServicoRequest
            {
                OrdemServicoId = Guid.Empty,
                PecaId = Guid.Empty,
                Quantidade = 1
            };

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
        }
    }
}
