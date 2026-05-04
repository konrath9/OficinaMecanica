using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.Domain
{
    public class ItemPecaTests
    {
        [Fact]
        public void Criar_ComDadosValidos_DeveCriarItem()
        {
            var id = Guid.NewGuid();
            var item = new ItemPeca(id, "OL-001", "Óleo 5W30", 45m, 2);

            Assert.Equal(id, item.PecaId);
            Assert.Equal("OL-001", item.Codigo);
            Assert.Equal("Óleo 5W30", item.Descricao);
            Assert.Equal(45m, item.PrecoUnitario);
            Assert.Equal(2, item.Quantidade);
            Assert.Equal(90m, item.TotalPrice);
        }

        [Fact]
        public void Criar_PecaIdVazio_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemPeca(Guid.Empty, "OL-001", "Desc", 10m, 1));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_CodigoVazio_DeveLancarArgumentException(string codigo)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemPeca(Guid.NewGuid(), codigo, "Desc", 10m, 1));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_DescricaoVazia_DeveLancarArgumentException(string descricao)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemPeca(Guid.NewGuid(), "OL-001", descricao, 10m, 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Criar_PrecoZeroOuNegativo_DeveLancarArgumentException(decimal preco)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemPeca(Guid.NewGuid(), "OL-001", "Desc", preco, 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Criar_QuantidadeZeroOuNegativa_DeveLancarArgumentException(int qtd)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemPeca(Guid.NewGuid(), "OL-001", "Desc", 10m, qtd));
        }

        [Fact]
        public void AtualizarQuantidade_ComValorValido_DeveAtualizar()
        {
            var item = new ItemPeca(Guid.NewGuid(), "OL-001", "Desc", 45m, 1);

            item.AtualizarQuantidade(3);

            Assert.Equal(3, item.Quantidade);
            Assert.Equal(135m, item.TotalPrice);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AtualizarQuantidade_ComValorInvalido_DeveLancarArgumentException(int qtd)
        {
            var item = new ItemPeca(Guid.NewGuid(), "OL-001", "Desc", 10m, 1);

            Assert.Throws<ArgumentException>(() => item.AtualizarQuantidade(qtd));
        }
    }
}
