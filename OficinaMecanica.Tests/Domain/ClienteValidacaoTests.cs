using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Tests.Domain
{
    public class ClienteValidacaoTests
    {
        [Fact]
        public void Criar_EmailValido_DeveSalvarNormalizado()
        {
            var cliente = new Cliente("João", "529.982.247-25", "JOAO@EMAIL.COM");

            Assert.Equal("joao@email.com", cliente.Email);
        }

        [Theory]
        [InlineData("email-invalido")]
        [InlineData("sem-arroba.com")]
        [InlineData("@dominio.com")]
        public void Criar_EmailInvalido_DeveLancarArgumentException(string email)
        {
            Assert.Throws<ArgumentException>(() =>
                new Cliente("João", "529.982.247-25", email));
        }

        [Theory]
        [InlineData("11999999999")]   // celular com DDD
        [InlineData("1133334444")]    // fixo com DDD
        [InlineData("(11) 99999-9999")] // com máscara
        public void Criar_TelefoneValido_DeveSalvarApenasDigitos(string telefone)
        {
            var cliente = new Cliente("João", "529.982.247-25", null, telefone);

            Assert.NotNull(cliente.Telefone);
            Assert.All(cliente.Telefone!, c => Assert.True(char.IsDigit(c)));
        }

        [Theory]
        [InlineData("123")]       // muito curto
        [InlineData("119999999999")] // 12 dígitos
        public void Criar_TelefoneInvalido_DeveLancarArgumentException(string telefone)
        {
            Assert.Throws<ArgumentException>(() =>
                new Cliente("João", "529.982.247-25", null, telefone));
        }

        [Fact]
        public void Criar_SemEmailESemTelefone_DeveCriarNormalmente()
        {
            var cliente = new Cliente("João", "529.982.247-25");

            Assert.Null(cliente.Email);
            Assert.Null(cliente.Telefone);
        }

        [Fact]
        public void AtualizarDocumento_ComDocumentoValido_DeveAtualizar()
        {
            var cliente = new Cliente("João", "529.982.247-25");

            cliente.AtualizarDocumento("111.444.777-35");

            Assert.Equal("11144477735", cliente.Documento);
            Assert.NotNull(cliente.UpdatedAt);
        }

        [Fact]
        public void AtualizarDocumento_ComDocumentoInvalido_DeveLancarArgumentException()
        {
            var cliente = new Cliente("João", "529.982.247-25");

            Assert.Throws<ArgumentException>(() => cliente.AtualizarDocumento("111.111.111-11"));
        }
    }
}
