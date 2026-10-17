using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Contracts;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.UseCases
{
    internal static class OsFabrica
    {
        public static OrdemServico EmDiagnosticoComServico()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m, 1));
            return os;
        }

        public static OrdemServico AguardandoAprovacao()
        {
            var os = EmDiagnosticoComServico();
            os.EnviarParaAprovacao();
            return os;
        }

        public static OrdemServico AguardandoPagamento()
        {
            var os = AguardandoAprovacao();
            os.Aprovar();
            return os;
        }

        public static OrdemServico EmExecucao()
        {
            var os = AguardandoPagamento();
            os.ConfirmarPagamento();
            return os;
        }

        public static NotificadorStatusOrdemServico Notificador(Mock<IClienteRepository> clientes, Mock<IEmailService> email) =>
            new(clientes.Object, email.Object, new OrdemServicoMetrics(), Mock.Of<ILogger<NotificadorStatusOrdemServico>>());
    }

    // ConcluirDiagnosticoUseCase - inicia a Saga
    public class ConcluirDiagnosticoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<IPublishEndpoint> _publishMock = new();

        private ConcluirDiagnosticoUseCase CriarUseCase() =>
            new(_repoMock.Object, _publishMock.Object, Mock.Of<ILogger<ConcluirDiagnosticoUseCase>>());

        [Fact]
        public async Task HandleAsync_OsEmDiagnosticoComItens_DevePublicarDiagnosticoConcluidoESalvar()
        {
            var os = OsFabrica.EmDiagnosticoComServico();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(os.Id);

            // O status só muda quando o Billing confirmar o orçamento (OrcamentoGerado)
            Assert.Equal(StatusOrdemServico.EmDiagnostico, response.Status);
            _publishMock.Verify(p => p.Publish(It.Is<DiagnosticoConcluido>(m => m.OrdemServicoId == os.Id), default), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OsRecebida_DeveLancarValidationExceptionSemPublicar()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await Assert.ThrowsAsync<ValidationException>(() => CriarUseCase().HandleAsync(os.Id));
            _publishMock.Verify(p => p.Publish(It.IsAny<DiagnosticoConcluido>(), default), Times.Never);
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

    // AprovarOrcamentoUseCase - cliente aprova, Saga pede a cobrança
    public class AprovarOrcamentoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<IPublishEndpoint> _publishMock = new();

        private AprovarOrcamentoUseCase CriarUseCase() =>
            new(_repoMock.Object, _publishMock.Object, OsFabrica.Notificador(_clienteRepoMock, _emailMock),
                Mock.Of<ILogger<AprovarOrcamentoUseCase>>());

        [Fact]
        public async Task HandleAsync_OsAguardandoAprovacao_DeveIrParaAguardandoPagamentoEPublicar()
        {
            var os = OsFabrica.AguardandoAprovacao();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(os.Id);

            Assert.Equal(StatusOrdemServico.AguardandoPagamento, response.Status);
            _publishMock.Verify(p => p.Publish(It.Is<OrcamentoAprovadoPeloCliente>(m => m.OrdemServicoId == os.Id), default), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteComEmail_DeveEnviarNotificacaoDeStatus()
        {
            var os = OsFabrica.AguardandoAprovacao();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default))
                .ReturnsAsync(new Cliente("João", "529.982.247-25", "joao@email.com"));

            await CriarUseCase().HandleAsync(os.Id);

            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(
                It.Is<NotificacaoStatusOrdemServico>(n => n.StatusAtual == StatusOrdemServico.AguardandoPagamento), default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_OsEmStatusErrado_DeveLancarValidationException()
        {
            var os = OsFabrica.EmDiagnosticoComServico();
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

    // SagaOrdemServicoAcoes - efeitos de cada passo da Saga na OS
    public class SagaOrdemServicoAcoesTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IEmailService> _emailMock = new();

        private SagaOrdemServicoAcoes CriarAcoes() =>
            new(_repoMock.Object, _clienteRepoMock.Object, OsFabrica.Notificador(_clienteRepoMock, _emailMock),
                new OrdemServicoMetrics(), Mock.Of<ILogger<SagaOrdemServicoAcoes>>());

        private OrdemServico Registrar(OrdemServico os)
        {
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            return os;
        }

        [Fact]
        public async Task MontarGerarOrcamento_DeveIncluirServicosPecasEEmailDoCliente()
        {
            var os = Registrar(OsFabrica.EmDiagnosticoComServico());
            os.AdicionarPeca(new ItemPeca(Guid.NewGuid(), "OL-1", "Óleo 5W30", 45m, 2));
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default))
                .ReturnsAsync(new Cliente("João", "529.982.247-25", "joao@email.com"));

            var comando = await CriarAcoes().MontarGerarOrcamentoAsync(os.Id);

            Assert.Equal(os.Numero, comando.NumeroOs);
            Assert.Equal("joao@email.com", comando.EmailCliente);
            Assert.Equal(240m, comando.Valor);
            Assert.Equal(new[] { "Servico", "Peca" }, comando.Itens.Select(i => i.Tipo));
        }

        [Fact]
        public async Task EnviarParaAprovacao_OsEmDiagnostico_DeveMudarStatusENotificar()
        {
            var os = Registrar(OsFabrica.EmDiagnosticoComServico());
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default))
                .ReturnsAsync(new Cliente("João", "529.982.247-25", "joao@email.com"));

            await CriarAcoes().EnviarParaAprovacaoAsync(os.Id);

            Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(It.IsAny<NotificacaoStatusOrdemServico>(), default), Times.Once);
        }

        [Fact]
        public async Task EnviarParaAprovacao_Reprocessado_DeveSerIdempotente()
        {
            var os = Registrar(OsFabrica.AguardandoAprovacao());

            await CriarAcoes().EnviarParaAprovacaoAsync(os.Id);

            Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<OrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task RegistrarFalhaOrcamento_DeveManterEmDiagnosticoERegistrarHistorico()
        {
            var os = Registrar(OsFabrica.EmDiagnosticoComServico());

            await CriarAcoes().RegistrarFalhaOrcamentoAsync(os.Id, "valor invalido");

            Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
            Assert.Contains(os.HistoricoStatus, h => h.Observacao != null && h.Observacao.Contains("valor invalido"));
        }

        [Fact]
        public async Task RegistrarLinkPagamento_DeveGuardarLinkNaOs()
        {
            var os = Registrar(OsFabrica.AguardandoPagamento());

            await CriarAcoes().RegistrarLinkPagamentoAsync(os.Id, "https://mp/checkout/1");

            Assert.Equal("https://mp/checkout/1", os.LinkPagamento);
        }

        [Fact]
        public async Task ConfirmarPagamento_DeveIrParaEmExecucaoERetornarComandoComServicos()
        {
            var os = Registrar(OsFabrica.AguardandoPagamento());

            var comando = await CriarAcoes().ConfirmarPagamentoAsync(os.Id);

            Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
            Assert.Equal(os.Id, comando.OrdemServicoId);
            Assert.Single(comando.Servicos);
        }

        [Fact]
        public async Task ConfirmarPagamento_Reprocessado_NaoDeveAlterarOsMasDeveRetornarComando()
        {
            var os = Registrar(OsFabrica.EmExecucao());

            var comando = await CriarAcoes().ConfirmarPagamentoAsync(os.Id);

            Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
            Assert.Equal(os.Numero, comando.NumeroOs);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<OrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task Finalizar_OsEmExecucao_DeveFinalizar()
        {
            var os = Registrar(OsFabrica.EmExecucao());

            await CriarAcoes().FinalizarAsync(os.Id);

            Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
            Assert.NotNull(os.FinalizadaEm);
        }

        [Fact]
        public async Task Finalizar_OsJaFinalizada_DeveSerIdempotente()
        {
            var os = Registrar(OsFabrica.EmExecucao());
            os.Finalizar();

            await CriarAcoes().FinalizarAsync(os.Id);

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<OrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task Cancelar_OsEmExecucao_DeveCancelarComMotivo()
        {
            var os = Registrar(OsFabrica.EmExecucao());

            await CriarAcoes().CancelarAsync(os.Id, "Falha na execução - pagamento estornado");

            Assert.Equal(StatusOrdemServico.Cancelada, os.Status);
            Assert.Contains("pagamento estornado", os.Observacoes);
        }

        [Fact]
        public async Task Cancelar_OsJaCancelada_DeveSerIdempotente()
        {
            var os = Registrar(OsFabrica.AguardandoAprovacao());
            os.Cancelar("cliente reprovou");

            await CriarAcoes().CancelarAsync(os.Id, "estorno");

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<OrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task OsInexistente_DeveLancarNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((OrdemServico?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CriarAcoes().FinalizarAsync(Guid.NewGuid()));
        }
    }

    // ?????????????????????????????????????????????
    // RegistrarEntregaUseCase
    // ?????????????????????????????????????????????
    public class RegistrarEntregaUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly OrdemServicoMetrics _metrics = new();
        private readonly Mock<ILogger<RegistrarEntregaUseCase>> _loggerMock = new();

        private RegistrarEntregaUseCase CriarUseCase() =>
            new(_repoMock.Object, _clienteRepoMock.Object, _emailMock.Object, _metrics, _loggerMock.Object);

        private static OrdemServico OsFinalizada()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Alinhamento", 120m, 1));
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.ConfirmarPagamento();
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
        public async Task HandleAsync_ClienteComEmail_DeveEnviarNotificacaoDeStatus()
        {
            var os = OsFinalizada();
            var cliente = new Cliente("Jo�o", "529.982.247-25", "joao@email.com");
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default)).ReturnsAsync(cliente);

            await CriarUseCase().HandleAsync(os.Id);

            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(
                It.Is<NotificacaoStatusOrdemServico>(n => n.DestinatarioEmail == "joao@email.com" && n.StatusAtual == StatusOrdemServico.Entregue),
                default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteSemEmail_NaoDeveEnviarNotificacao()
        {
            var os = OsFinalizada();
            var cliente = new Cliente("Jo�o", "529.982.247-25");
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default)).ReturnsAsync(cliente);

            await CriarUseCase().HandleAsync(os.Id);

            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(It.IsAny<NotificacaoStatusOrdemServico>(), default), Times.Never);
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
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<IPublishEndpoint> _publishMock = new();
        private readonly Mock<ILogger<CancelarOrdemServicoUseCase>> _loggerMock = new();

        private CancelarOrdemServicoUseCase CriarUseCase() =>
            new(_repoMock.Object, _publishMock.Object,
                new NotificadorStatusOrdemServico(_clienteRepoMock.Object, _emailMock.Object, new OrdemServicoMetrics(), Mock.Of<ILogger<NotificadorStatusOrdemServico>>()),
                _loggerMock.Object);

        [Fact]
        public async Task HandleAsync_OsRecebida_DeveAlterarParaCancelada()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            var response = await CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(os.Id, "Desist�ncia do cliente"));

            Assert.Equal(StatusOrdemServico.Cancelada, response.Status);
            _repoMock.Verify(r => r.UpdateAsync(os, default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteComEmail_DeveEnviarNotificacaoComMotivo()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            var cliente = new Cliente("Jo�o", "529.982.247-25", "joao@email.com");
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default)).ReturnsAsync(cliente);

            await CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(os.Id, "Desist�ncia do cliente"));

            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(
                It.Is<NotificacaoStatusOrdemServico>(n =>
                    n.DestinatarioEmail == "joao@email.com" &&
                    n.StatusAtual == StatusOrdemServico.Cancelada &&
                    n.Observacao == "Desist�ncia do cliente"),
                default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ClienteSemEmail_NaoDeveEnviarNotificacao()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            var cliente = new Cliente("Jo�o", "529.982.247-25");
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default)).ReturnsAsync(cliente);

            await CriarUseCase().HandleAsync(new CancelarOrdemServicoRequest(os.Id, "Motivo"));

            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(It.IsAny<NotificacaoStatusOrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_OsFinalizada_DeveLancarValidationException()
        {
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(Guid.NewGuid(), "Servi�o", 100m, 1));
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.ConfirmarPagamento();
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
            var servico = new Servico("Troca de �leo", "Desc", 150m);

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
            var peca = new Peca("OL-001", "�leo 5W30", 45m, 10);

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
            var peca = new Peca("OL-001", "�leo", 45m, 1);

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
