using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Tests.Domain
{
    public class ClienteTests
    {
        // ??????????????????????????????????????????????
        // Cria��o v�lida
        // ??????????????????????????????????????????????

        [Fact]
        public void Criar_ComCpfValido_DeveCriarCliente()
        {
            var cliente = new Cliente("Jo�o Silva", "529.982.247-25", "joao@email.com", "11999999999");

            Assert.NotEqual(Guid.Empty, cliente.Id);
            Assert.Equal("Jo�o Silva", cliente.Nome);
            Assert.Equal("52998224725", cliente.Documento); // normalizado
            Assert.Equal("joao@email.com", cliente.Email);
        }

        [Fact]
        public void Criar_ComCnpjValido_DeveCriarCliente()
        {
            var cliente = new Cliente("Empresa LTDA", "11.222.333/0001-81");

            Assert.NotEqual(Guid.Empty, cliente.Id);
            Assert.Equal("11222333000181", cliente.Documento);
        }

        [Fact]
        public void Criar_DocumentoNormalizado_DeveSalvarSomentedigitos()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");

            Assert.Equal("52998224725", cliente.Documento);
            Assert.All(cliente.Documento, c => Assert.True(char.IsDigit(c)));
        }

        [Fact]
        public void Criar_DeveIniciarAtivo()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");

            Assert.True(cliente.Ativo);
        }

        // ??????????????????????????????????????????????
        // Cria��o inv�lida
        // ??????????????????????????????????????????????

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_NomeVazio_DeveLancarArgumentException(string nome)
        {
            Assert.Throws<ArgumentException>(() =>
                new Cliente(nome, "529.982.247-25"));
        }

        [Fact]
        public void Criar_CpfInvalido_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Cliente("Jo�o", "111.111.111-11"));
        }

        [Fact]
        public void Criar_CnpjInvalido_DeveLancarArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Cliente("Empresa", "11.111.111/1111-11"));
        }

        // ??????????????????????????????????????????????
        // Atualiza��o
        // ??????????????????????????????????????????????

        [Fact]
        public void Atualizar_ComDadosValidos_DeveAtualizarCampos()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");

            cliente.Atualizar("Jo�o Atualizado", "novo@email.com", "11988888888");

            Assert.Equal("Jo�o Atualizado", cliente.Nome);
            Assert.Equal("novo@email.com", cliente.Email);
            Assert.NotNull(cliente.UpdatedAt);
        }

        [Fact]
        public void Atualizar_NomeVazio_DeveLancarArgumentException()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");

            Assert.Throws<ArgumentException>(() => cliente.Atualizar("", null, null));
        }

        // ??????????????????????????????????????????????
        // Ativar / Desativar
        // ??????????????????????????????????????????????

        [Fact]
        public void Desativar_ClienteAtivo_DeveFicarInativo()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");

            cliente.Desativar();

            Assert.False(cliente.Ativo);
            Assert.NotNull(cliente.UpdatedAt);
        }

        [Fact]
        public void Desativar_ClienteJaInativo_DeveLancarInvalidOperationException()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");
            cliente.Desativar();

            Assert.Throws<InvalidOperationException>(() => cliente.Desativar());
        }

        [Fact]
        public void Ativar_ClienteInativo_DeveFicarAtivo()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");
            cliente.Desativar();

            cliente.Ativar();

            Assert.True(cliente.Ativo);
        }

        [Fact]
        public void Ativar_ClienteJaAtivo_DeveLancarInvalidOperationException()
        {
            var cliente = new Cliente("Jo�o", "529.982.247-25");

            Assert.Throws<InvalidOperationException>(() => cliente.Ativar());
        }
    }
}
