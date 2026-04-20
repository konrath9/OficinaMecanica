using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.Domain
{
    public class ServicoTests
    {
        [Fact]
        public void Criar_ComDadosValidos_DeveCriarServico()
        {
            var servico = new Servico("Troca de óleo", "Troca de óleo do motor", 150m);

            Assert.NotEqual(Guid.Empty, servico.Id);
            Assert.Equal("Troca de óleo", servico.Nome);
            Assert.Equal("Troca de óleo do motor", servico.Descricao);
            Assert.Equal(150m, servico.Preco);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_NomeVazio_DeveLancarArgumentException(string nome)
        {
            Assert.Throws<ArgumentException>(() =>
                new Servico(nome, "descricao", 100m));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_DescricaoVazia_DeveLancarArgumentException(string descricao)
        {
            Assert.Throws<ArgumentException>(() =>
                new Servico("Servico", descricao, 100m));
        }

        [Fact]
        public void Criar_PrecoNegativo_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Servico("Servico", "Descricao", -1m));
        }

        [Fact]
        public void Atualizar_ComDadosValidos_DeveAtualizarCampos()
        {
            var servico = new Servico("Original", "Desc original", 100m);

            servico.Atualizar("Atualizado", "Desc atualizada", 200m);

            Assert.Equal("Atualizado", servico.Nome);
            Assert.Equal("Desc atualizada", servico.Descricao);
            Assert.Equal(200m, servico.Preco);
            Assert.NotNull(servico.UpdatedAt);
        }

        [Fact]
        public void Atualizar_NomeVazio_DeveLancarArgumentException()
        {
            var servico = new Servico("Original", "Desc", 100m);

            Assert.Throws<ArgumentException>(() =>
                servico.Atualizar("", "Desc", 100m));
        }

        [Fact]
        public void Atualizar_DescricaoVazia_DeveLancarArgumentException()
        {
            var servico = new Servico("Original", "Desc", 100m);

            Assert.Throws<ArgumentException>(() =>
                servico.Atualizar("Nome", "", 100m));
        }

        [Fact]
        public void Atualizar_PrecoNegativo_DeveLancarArgumentException()
        {
            var servico = new Servico("Original", "Desc", 100m);

            Assert.Throws<ArgumentException>(() =>
                servico.Atualizar("Nome", "Desc", -5m));
        }
    }
}
