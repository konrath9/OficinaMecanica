using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.Domain
{
    public class OrdemServicoTests
    {
        private static OrdemServico CriarOsValida() =>
            new("OS-001", Guid.NewGuid(), Guid.NewGuid());

        private static ItemServico CriarItemServico() =>
            new(Guid.NewGuid(), "Troca de óleo", 150m, 1);

        private static ItemPeca CriarItemPeca() =>
            new(Guid.NewGuid(), "OL-001", "Óleo 5W30", 45m, 2);

        // ??????????????????????????????????????????????
        // Criação
        // ??????????????????????????????????????????????

        [Fact]
        public void Criar_ComDadosValidos_DeveCriarComStatusRecebida()
        {
            var os = CriarOsValida();

            Assert.NotEqual(Guid.Empty, os.Id);
            Assert.Equal("OS-001", os.Numero);
            Assert.Equal(StatusOrdemServico.Recebida, os.Status);
            Assert.Empty(os.Servicos);
            Assert.Empty(os.Pecas);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_NumeroVazio_DeveLancarArgumentException(string numero)
        {
            Assert.Throws<ArgumentException>(() =>
                new OrdemServico(numero, Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public void Criar_ClienteIdVazio_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new OrdemServico("OS-001", Guid.Empty, Guid.NewGuid()));
        }

        [Fact]
        public void Criar_VeiculoIdVazio_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new OrdemServico("OS-001", Guid.NewGuid(), Guid.Empty));
        }

        // ??????????????????????????????????????????????
        // Serviços e Peças
        // ??????????????????????????????????????????????

        [Fact]
        public void AdicionarServico_EmOsRecebida_DeveAdicionarComSucesso()
        {
            var os = CriarOsValida();
            var item = CriarItemServico();

            os.AdicionarServico(item);

            Assert.Single(os.Servicos);
            Assert.Equal(150m, os.TotalServicos);
        }

        [Fact]
        public void AdicionarPeca_EmOsRecebida_DeveAdicionarComSucesso()
        {
            var os = CriarOsValida();
            var item = CriarItemPeca();

            os.AdicionarPeca(item);

            Assert.Single(os.Pecas);
            Assert.Equal(90m, os.TotalPecas); // 45 * 2
        }

        [Fact]
        public void TotalOrcamento_DeveSerSomaDosServicosEPecas()
        {
            var os = CriarOsValida();
            os.AdicionarServico(CriarItemServico()); // 150
            os.AdicionarPeca(CriarItemPeca());       // 90

            Assert.Equal(240m, os.TotalOrcamento);
        }

        [Fact]
        public void AdicionarServico_EmOsFinalizada_DeveLancarInvalidOperationException()
        {
            var os = CriarOsValida();
            os.AdicionarServico(CriarItemServico());
            os.IniciarDiagnostico();
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.Finalizar();

            Assert.Throws<InvalidOperationException>(() => os.AdicionarServico(CriarItemServico()));
        }

        // ??????????????????????????????????????????????
        // Fluxo de status — caminho feliz
        // ??????????????????????????????????????????????

        [Fact]
        public void FluxoCompleto_DevePercorrerTodosOsStatusAteEntregue()
        {
            var os = CriarOsValida();
            Assert.Equal(StatusOrdemServico.Recebida, os.Status);

            os.IniciarDiagnostico();
            Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
            Assert.NotNull(os.IniciadaEm);

            os.AdicionarServico(CriarItemServico());
            os.EnviarParaAprovacao();
            Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);

            os.Aprovar();
            Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);

            os.Finalizar();
            Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
            Assert.NotNull(os.FinalizadaEm);

            os.Entregar();
            Assert.Equal(StatusOrdemServico.Entregue, os.Status);
            Assert.NotNull(os.EntregueEm);
        }

        // ??????????????????????????????????????????????
        // Transições inválidas
        // ??????????????????????????????????????????????

        [Fact]
        public void EnviarParaAprovacao_SemServicosNemPecas_DeveLancarInvalidOperationException()
        {
            var os = CriarOsValida();
            os.IniciarDiagnostico();

            Assert.Throws<InvalidOperationException>(() => os.EnviarParaAprovacao());
        }

        [Fact]
        public void IniciarDiagnostico_EmOsNaoRecebida_DeveLancarInvalidOperationException()
        {
            var os = CriarOsValida();
            os.IniciarDiagnostico();

            Assert.Throws<InvalidOperationException>(() => os.IniciarDiagnostico());
        }

        [Fact]
        public void Finalizar_EmOsNaoEmExecucao_DeveLancarInvalidOperationException()
        {
            var os = CriarOsValida();

            Assert.Throws<InvalidOperationException>(() => os.Finalizar());
        }

        [Fact]
        public void Cancelar_OsFinalizada_DeveLancarInvalidOperationException()
        {
            var os = CriarOsValida();
            os.AdicionarServico(CriarItemServico());
            os.IniciarDiagnostico();
            os.EnviarParaAprovacao();
            os.Aprovar();
            os.Finalizar();

            Assert.Throws<InvalidOperationException>(() => os.Cancelar());
        }

        [Fact]
        public void Cancelar_OsRecebida_DeveAlterarStatusParaCancelada()
        {
            var os = CriarOsValida();
            os.Cancelar("Desistência do cliente");

            Assert.Equal(StatusOrdemServico.Cancelada, os.Status);
        }
    }
}
