using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.Domain
{
    public class PecaTests
    {
        [Fact]
        public void Criar_ComDadosValidos_DeveCriarPeca()
        {
            var peca = new Peca("OL-001", "Óleo 5W30", 45m, 10);

            Assert.NotEqual(Guid.Empty, peca.Id);
            Assert.Equal("OL-001", peca.Codigo);
            Assert.Equal("Óleo 5W30", peca.Nome);
            Assert.Equal(45m, peca.PrecoUnitario);
            Assert.Equal(10, peca.QuantidadeEstoque);
        }

        [Fact]
        public void Criar_CodigoDeveSerNormalizadoParaMaiusculas()
        {
            var peca = new Peca("ol-001", "Óleo", 10m);

            Assert.Equal("OL-001", peca.Codigo);
        }

        [Fact]
        public void Criar_SemEstoqueInicial_DeveIniciarComZero()
        {
            var peca = new Peca("OL-001", "Óleo", 10m);

            Assert.Equal(0, peca.QuantidadeEstoque);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_CodigoVazio_DeveLancarArgumentException(string codigo)
        {
            Assert.Throws<ArgumentException>(() =>
                new Peca(codigo, "Óleo", 10m));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_NomeVazio_DeveLancarArgumentException(string nome)
        {
            Assert.Throws<ArgumentException>(() =>
                new Peca("OL-001", nome, 10m));
        }

        [Fact]
        public void Criar_PrecoNegativo_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Peca("OL-001", "Óleo", -1m));
        }

        [Fact]
        public void Criar_EstoqueNegativo_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Peca("OL-001", "Óleo", 10m, -5));
        }

        // Atualizar

        [Fact]
        public void Atualizar_ComDadosValidos_DeveAtualizarCampos()
        {
            var peca = new Peca("OL-001", "Óleo", 45m, 10);

            peca.Atualizar("Óleo Sintético", 80m);

            Assert.Equal("Óleo Sintético", peca.Nome);
            Assert.Equal(80m, peca.PrecoUnitario);
            Assert.NotNull(peca.UpdatedAt);
        }

        [Fact]
        public void Atualizar_NomeVazio_DeveLancarArgumentException()
        {
            var peca = new Peca("OL-001", "Óleo", 10m);

            Assert.Throws<ArgumentException>(() => peca.Atualizar("", 10m));
        }

        [Fact]
        public void Atualizar_PrecoNegativo_DeveLancarArgumentException()
        {
            var peca = new Peca("OL-001", "Óleo", 10m);

            Assert.Throws<ArgumentException>(() => peca.Atualizar("Óleo", -1m));
        }

        // Estoque

        [Fact]
        public void EntradaEstoque_DeveIncrementarQuantidade()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 5);

            peca.EntradaEstoque(10);

            Assert.Equal(15, peca.QuantidadeEstoque);
        }

        [Fact]
        public void EntradaEstoque_QuantidadeZero_DeveLancarArgumentException()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 5);

            Assert.Throws<ArgumentException>(() => peca.EntradaEstoque(0));
        }

        [Fact]
        public void EntradaEstoque_QuantidadeNegativa_DeveLancarArgumentException()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 5);

            Assert.Throws<ArgumentException>(() => peca.EntradaEstoque(-3));
        }

        [Fact]
        public void SaidaEstoque_ComEstoqueSuficiente_DeveDecrementarQuantidade()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 10);

            peca.SaidaEstoque(3);

            Assert.Equal(7, peca.QuantidadeEstoque);
        }

        [Fact]
        public void SaidaEstoque_EstoqueInsuficiente_DeveLancarInvalidOperationException()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 2);

            Assert.Throws<InvalidOperationException>(() => peca.SaidaEstoque(5));
        }

        [Fact]
        public void SaidaEstoque_QuantidadeZero_DeveLancarArgumentException()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 5);

            Assert.Throws<ArgumentException>(() => peca.SaidaEstoque(0));
        }

        [Fact]
        public void TemEstoque_ComEstoqueSuficiente_DeveRetornarTrue()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 5);

            Assert.True(peca.TemEstoque(3));
        }

        [Fact]
        public void TemEstoque_ComEstoqueInsuficiente_DeveRetornarFalse()
        {
            var peca = new Peca("OL-001", "Óleo", 10m, 2);

            Assert.False(peca.TemEstoque(5));
        }
    }
}
