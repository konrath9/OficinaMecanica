using Microsoft.Extensions.Logging;
using Moq;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.UseCases.OrdemServico;
using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using System.Reflection;

namespace OficinaMecanica.Tests.UseCases
{
    public class ObterOrdemServicoUseCaseTests
    {
        private readonly Mock<IOrdemServicoRepository> _repoMock = new();
        private readonly Mock<IClienteRepository> _clienteRepoMock = new();
        private readonly Mock<IVeiculoRepository> _veiculoRepoMock = new();
        private readonly Mock<ILogger<ObterOrdemServicoUseCase>> _loggerMock = new();

        private ObterOrdemServicoUseCase CriarUseCase() =>
            new(_repoMock.Object, _clienteRepoMock.Object, _veiculoRepoMock.Object, _loggerMock.Object);

        private static OrdemServico CriarOs(StatusOrdemServico status, DateTime criadaEm)
        {
            var os = new OrdemServico($"OS-{Guid.NewGuid():N}", Guid.NewGuid(), Guid.NewGuid());
            typeof(Entity).GetProperty(nameof(Entity.CreatedAt), BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(os, criadaEm);

            switch (status)
            {
                case StatusOrdemServico.Recebida:
                    break;
                case StatusOrdemServico.EmDiagnostico:
                    os.IniciarDiagnostico();
                    break;
                case StatusOrdemServico.AguardandoAprovacao:
                    os.IniciarDiagnostico();
                    os.AdicionarServico(new OficinaMecanica.Domain.ValueObjects.ItemServico(Guid.NewGuid(), "Servico", 100m, 1));
                    os.EnviarParaAprovacao();
                    break;
                case StatusOrdemServico.EmExecucao:
                    os.IniciarDiagnostico();
                    os.AdicionarServico(new OficinaMecanica.Domain.ValueObjects.ItemServico(Guid.NewGuid(), "Servico", 100m, 1));
                    os.EnviarParaAprovacao();
                    os.Aprovar();
                    break;
                case StatusOrdemServico.Finalizada:
                    os.IniciarDiagnostico();
                    os.AdicionarServico(new OficinaMecanica.Domain.ValueObjects.ItemServico(Guid.NewGuid(), "Servico", 100m, 1));
                    os.EnviarParaAprovacao();
                    os.Aprovar();
                    os.Finalizar();
                    break;
                case StatusOrdemServico.Entregue:
                    os.IniciarDiagnostico();
                    os.AdicionarServico(new OficinaMecanica.Domain.ValueObjects.ItemServico(Guid.NewGuid(), "Servico", 100m, 1));
                    os.EnviarParaAprovacao();
                    os.Aprovar();
                    os.Finalizar();
                    os.Entregar();
                    break;
                case StatusOrdemServico.Cancelada:
                    os.Cancelar("Motivo qualquer");
                    break;
            }

            return os;
        }

        [Fact]
        public async Task ListarAsync_DeveOrdenarPorPrioridadeDeStatus()
        {
            var baseData = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var recebida = CriarOs(StatusOrdemServico.Recebida, baseData);
            var diagnostico = CriarOs(StatusOrdemServico.EmDiagnostico, baseData);
            var aguardandoAprovacao = CriarOs(StatusOrdemServico.AguardandoAprovacao, baseData);
            var emExecucao = CriarOs(StatusOrdemServico.EmExecucao, baseData);

            _repoMock.Setup(r => r.GetAllAsync(default))
                .ReturnsAsync(new[] { recebida, diagnostico, aguardandoAprovacao, emExecucao });

            var resultado = (await CriarUseCase().ListarAsync()).ToList();

            Assert.Equal(4, resultado.Count);
            Assert.Equal(emExecucao.Id, resultado[0].Id);
            Assert.Equal(aguardandoAprovacao.Id, resultado[1].Id);
            Assert.Equal(diagnostico.Id, resultado[2].Id);
            Assert.Equal(recebida.Id, resultado[3].Id);
        }

        [Fact]
        public async Task ListarAsync_MesmoStatus_DeveOrdenarPelaMaisAntigaPrimeiro()
        {
            var maisAntiga = CriarOs(StatusOrdemServico.Recebida, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var maisRecente = CriarOs(StatusOrdemServico.Recebida, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));

            _repoMock.Setup(r => r.GetAllAsync(default))
                .ReturnsAsync(new[] { maisRecente, maisAntiga });

            var resultado = (await CriarUseCase().ListarAsync()).ToList();

            Assert.Equal(maisAntiga.Id, resultado[0].Id);
            Assert.Equal(maisRecente.Id, resultado[1].Id);
        }

        [Fact]
        public async Task ListarAsync_DeveExcluirOsFinalizadasEEntregues()
        {
            var baseData = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var finalizada = CriarOs(StatusOrdemServico.Finalizada, baseData);
            var entregue = CriarOs(StatusOrdemServico.Entregue, baseData);
            var emExecucao = CriarOs(StatusOrdemServico.EmExecucao, baseData);

            _repoMock.Setup(r => r.GetAllAsync(default))
                .ReturnsAsync(new[] { finalizada, entregue, emExecucao });

            var resultado = (await CriarUseCase().ListarAsync()).ToList();

            Assert.Single(resultado);
            Assert.Equal(emExecucao.Id, resultado[0].Id);
        }

        [Fact]
        public async Task ListarAsync_OsCancelada_DeveAparecerPorUltimo()
        {
            var baseData = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var recebida = CriarOs(StatusOrdemServico.Recebida, baseData);
            var cancelada = CriarOs(StatusOrdemServico.Cancelada, baseData);

            _repoMock.Setup(r => r.GetAllAsync(default))
                .ReturnsAsync(new[] { cancelada, recebida });

            var resultado = (await CriarUseCase().ListarAsync()).ToList();

            Assert.Equal(2, resultado.Count);
            Assert.Equal(recebida.Id, resultado[0].Id);
            Assert.Equal(cancelada.Id, resultado[1].Id);
        }
    }
}
