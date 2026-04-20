using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.Domain
{
    public class ItemServicoTests
    {
        [Fact]
        public void Criar_ComDadosValidos_DeveCriarItem()
        {
            var id = Guid.NewGuid();
            var item = new ItemServico(id, "Troca de óleo", 150m, 2);

            Assert.Equal(id, item.ServicoId);
            Assert.Equal("Troca de óleo", item.Descricao);
            Assert.Equal(150m, item.PrecoUnitario);
            Assert.Equal(2, item.Quantidade);
            Assert.Equal(300m, item.TotalPrice);
        }

        [Fact]
        public void Criar_ServicoIdVazio_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemServico(Guid.Empty, "Desc", 100m, 1));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_DescricaoVazia_DeveLancarArgumentException(string descricao)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemServico(Guid.NewGuid(), descricao, 100m, 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Criar_PrecoZeroOuNegativo_DeveLancarArgumentException(decimal preco)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemServico(Guid.NewGuid(), "Desc", preco, 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Criar_QuantidadeZeroOuNegativa_DeveLancarArgumentException(int qtd)
        {
            Assert.Throws<ArgumentException>(() =>
                new ItemServico(Guid.NewGuid(), "Desc", 100m, qtd));
        }

        [Fact]
        public void AtualizarQuantidade_ComValorValido_DeveAtualizar()
        {
            var item = new ItemServico(Guid.NewGuid(), "Desc", 100m, 1);

            item.AtualizarQuantidade(5);

            Assert.Equal(5, item.Quantidade);
            Assert.Equal(500m, item.TotalPrice);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AtualizarQuantidade_ComValorInvalido_DeveLancarArgumentException(int qtd)
        {
            var item = new ItemServico(Guid.NewGuid(), "Desc", 100m, 1);

            Assert.Throws<ArgumentException>(() => item.AtualizarQuantidade(qtd));
        }
    }
}
