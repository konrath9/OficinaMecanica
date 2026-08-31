using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.UseCases
{
    public class RegistrarExecucaoServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly OrdemServicoMetrics _metrics = new();
        private readonly Mock<ILogger<RegistrarExecucaoServicoUseCase>> _loggerMock = new();

        private RegistrarExecucaoServicoUseCase CriarUseCase() =>
            new(_repoMock.Object, _clienteRepoMock.Object, _emailMock.Object, _metrics, _loggerMock.Object);

        private static (OrdemServico Os, Guid ServicoId) OsEmExecucaoComUnicoServico()
        {
            var servicoId = Guid.NewGuid();
            var os = new OrdemServico("OS-001", Guid.NewGuid(), Guid.NewGuid());
            os.IniciarDiagnostico();
            os.AdicionarServico(new ItemServico(servicoId, "Troca de �leo", 150m, 1));
            os.EnviarParaAprovacao();
            os.Aprovar();
            return (os, servicoId);
        }

        [Fact]
        public async Task HandleAsync_FinalizarUnicoServico_DeveFinalizarOsAutomaticamenteEEnviarNotificacao()
        {
            var (os, servicoId) = OsEmExecucaoComUnicoServico();
            os.IniciarExecucaoServico(servicoId);
            var cliente = new Cliente("Jo�o", "529.982.247-25", "joao@email.com");
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);
            _clienteRepoMock.Setup(r => r.GetByIdAsync(os.ClienteId, default)).ReturnsAsync(cliente);

            var response = await CriarUseCase().HandleAsync(new RegistrarExecucaoServicoRequest(os.Id, servicoId, "finalizar"));

            Assert.True(response.OsFinalizadaAutomaticamente);
            Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(
                It.Is<NotificacaoStatusOrdemServico>(n => n.DestinatarioEmail == "joao@email.com" && n.StatusAtual == StatusOrdemServico.Finalizada),
                default), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Iniciar_NaoDeveEnviarNotificacao()
        {
            var (os, servicoId) = OsEmExecucaoComUnicoServico();
            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await CriarUseCase().HandleAsync(new RegistrarExecucaoServicoRequest(os.Id, servicoId, "iniciar"));

            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(It.IsAny<NotificacaoStatusOrdemServico>(), default), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_FinalizarServicoQueNaoEUltimo_NaoDeveEnviarNotificacao()
        {
            var (os, servicoId) = OsEmExecucaoComUnicoServico();
            var outroServicoId = Guid.NewGuid();
            os.AdicionarServico(new ItemServico(outroServicoId, "Alinhamento", 100m, 1));
            os.IniciarExecucaoServico(servicoId);

            _repoMock.Setup(r => r.GetByIdAsync(os.Id, default)).ReturnsAsync(os);

            await CriarUseCase().HandleAsync(new RegistrarExecucaoServicoRequest(os.Id, servicoId, "finalizar"));

            Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
            _emailMock.Verify(e => e.EnviarNotificacaoStatusAsync(It.IsAny<NotificacaoStatusOrdemServico>(), default), Times.Never);
        }
    }
}
