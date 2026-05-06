using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.UseCases
{
    // ?????????????????????????????????????????????
    // ConcluirDiagnosticoUseCase
    // ?????????????????????????????????????????????
    public class ConcluirDiagnosticoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<ConcluirDiagnosticoUseCase>> _loggerMock = new();

        private ConcluirDiagnosticoUseCase CriarUseCase() =>
            new(_repoMock.Object, _loggerMock.Object);

        private static OrdemServico OsEmDiagnosticoComServico()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m, 1));
            return os;
        }

        [Fact]
        public async Task HandleAsync_OsEmDiagnosticoComItens_DeveAvancarParaAguardandoAprovacao()
        {
            var os = OsEmDiagnosticoComServico();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(os.Id);

            Assert.Equal(StatusOrdemServico.AguardandoAprovacao, response.Status);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OsRecebida_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(os.Id));
        }

        [Fact]
        public async Task HandleAsync_OsEmDiagnosticoSemItens_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(os.Id));
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task HandleAsync_IdVazio_DeveLancarValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(Guid.Empty));
        }
    }

    // ?????????????????????????????????????????????
    // AprovarOrcamentoUseCase
    // ?????????????????????????????????????????????
    public class AprovarOrcamentoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<AprovarOrcamentoUseCase>> _loggerMock = new();

        private AprovarOrcamentoUseCase CriarUseCase() =>
            new(_repoMock.Object, _loggerMock.Object);

        private static OrdemServico OsAguardandoAprovacao()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Revisão", 200m, 1));
            os.EnviarParaAprovacao();
            return os;
        }

        [Fact]
        public async Task HandleAsync_OsAguardandoAprovacao_DeveAvancarParaEmExecucao()
        {
            var os = OsAguardandoAprovacao();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(os.Id);

            Assert.Equal(StatusOrdemServico.EmExecucao, response.Status);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OsEmStatusErrado_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(os.Id));
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task HandleAsync_IdVazio_DeveLancarValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(Guid.Empty));
        }
    }

    // ?????????????????????????????????????????????
    // RegistrarEntregaUseCase
    // ?????????????????????????????????????????????
    public class RegistrarEntregaUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<RegistrarEntregaUseCase>> _loggerMock = new();

        private RegistrarEntregaUseCase CriarUseCase() =>
            new(_repoMock.Object, _loggerMock.Object);

        private static OrdemServico OsFinalizada()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Alinhamento", 120m, 1));
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.Finalizar();
            return os;
        }

        [Fact]
        public async Task HandleAsync_OsFinalizada_DeveAvancarParaEntregue()
        {
            var os = OsFinalizada();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(os.Id);

            Assert.Equal(StatusOrdemServico.Entregue, response.Status);
            Assert.NotNull(response.EntregueEm);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OsNaoFinalizada_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(os.Id));
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarUseCase().HandleAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task HandleAsync_IdVazio_DeveLancarValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(Guid.Empty));
        }
    }

    // ?????????????????????????????????????????????
    // CancelarOrdemServicoUseCase
    // ?????????????????????????????????????????????
    public class CancelarOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<ILogger<CancelarOrdemServicoUseCase>> _loggerMock = new();

        private CancelarOrdemServicoUseCase CriarUseCase() =>
            new(_repoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_OsRecebida_DeveAlterarParaCancelada()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(os.Id, "Desistência do cliente"));

            Assert.Equal(StatusOrdemServico.Cancelada, response.Status);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OsFinalizada_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Serviço", 100m, 1));
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.Finalizar();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(os.Id, "Motivo")));
        }

        [Fact]
        public async Task HandleAsync_OSNaoEncontrada_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(Guid.NewGuid())));
        }

        [Fact]
        public async Task HandleAsync_IdVazio_DeveLancarValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(() =>
                CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(Guid.Empty)));
        }
    }

    // ?????????????????????????????????????????????
    // AdicionarServicoOrdemServicoUseCase
    // ?????????????????????????????????????????????
    public class AdicionarServicoOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepoMock = new();
        private readonly Mock<IServicoRepository> _servicoRepoMock = new();
        private readonly Mock<ILogger<AdicionarServicoOrdemServicoUseCase>> _loggerMock = new();

        private AdicionarServicoOrdemServicoUseCase CriarUseCase() =>
            new(_osRepoMock.Object, _servicoRepoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAdicionarServicoEAvancarParaDiagnostico()
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
            Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
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

    // ?????????????????????????????????????????????
    // AdicionarPecaOrdemServicoUseCase
    // ?????????????????????????????????????????????
    public class AdicionarPecaOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _osRepoMock = new();
        private readonly Mock<IPecaRepository> _pecaRepoMock = new();
        private readonly Mock<ILogger<AdicionarPecaOrdemServicoUseCase>> _loggerMock = new();

        private AdicionarPecaOrdemServicoUseCase CriarUseCase() =>
            new(_osRepoMock.Object, _pecaRepoMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_ComDadosValidos_DeveAdicionarPecaBaixarEstoqueEAvancarParaDiagnostico()
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
            Assert.Equal(8, peca.QuantidadeEstoque);
            Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
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

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(request));
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
