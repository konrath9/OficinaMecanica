using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Tests.Domain
{
    public class UsuarioTests
    {
        [Fact]
        public void Criar_ComDadosValidos_DeveCriarUsuarioAtivo()
        {
            var usuario = new Usuario("Admin", "admin@oficina.com", "hash", PerfilUsuario.Administrador);

            Assert.NotEqual(Guid.Empty, usuario.Id);
            Assert.Equal("Admin", usuario.Nome);
            Assert.Equal("admin@oficina.com", usuario.Email);
            Assert.Equal(PerfilUsuario.Administrador, usuario.Perfil);
            Assert.True(usuario.Ativo);
        }

        [Fact]
        public void Criar_EmailDeveSerNormalizadoParaMinusculas()
        {
            var usuario = new Usuario("Admin", "ADMIN@OFICINA.COM", "hash", PerfilUsuario.Administrador);

            Assert.Equal("admin@oficina.com", usuario.Email);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_NomeVazio_DeveLancarArgumentException(string nome)
        {
            Assert.Throws<ArgumentException>(() =>
                new Usuario(nome, "admin@oficina.com", "hash", PerfilUsuario.Administrador));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_EmailVazio_DeveLancarArgumentException(string email)
        {
            Assert.Throws<ArgumentException>(() =>
                new Usuario("Admin", email, "hash", PerfilUsuario.Administrador));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_SenhaVazia_DeveLancarArgumentException(string senha)
        {
            Assert.Throws<ArgumentException>(() =>
                new Usuario("Admin", "admin@oficina.com", senha, PerfilUsuario.Administrador));
        }

        [Fact]
        public void Desativar_UsuarioAtivo_DeveDesativar()
        {
            var usuario = new Usuario("Admin", "admin@oficina.com", "hash", PerfilUsuario.Administrador);

            usuario.Desativar();

            Assert.False(usuario.Ativo);
            Assert.NotNull(usuario.UpdatedAt);
        }

        [Fact]
        public void AtualizarSenha_ComSenhaValida_DeveAtualizar()
        {
            var usuario = new Usuario("Admin", "admin@oficina.com", "hash_antigo", PerfilUsuario.Administrador);

            usuario.AtualizarSenha("hash_novo");

            Assert.Equal("hash_novo", usuario.SenhaHash);
            Assert.NotNull(usuario.UpdatedAt);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void AtualizarSenha_SenhaVazia_DeveLancarArgumentException(string senha)
        {
            var usuario = new Usuario("Admin", "admin@oficina.com", "hash", PerfilUsuario.Administrador);

            Assert.Throws<ArgumentException>(() => usuario.AtualizarSenha(senha));
        }
    }
}
