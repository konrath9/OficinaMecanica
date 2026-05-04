using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa um usuário do sistema administrativo da oficina.
    /// </summary>
    public class Usuario : Entity
    {
        public string Nome { get; private set; }
        public string Email { get; private set; }
        public string SenhaHash { get; private set; }
        public PerfilUsuario Perfil { get; private set; }
        public bool Ativo { get; private set; }

        private Usuario() { }

        public Usuario(string nome, string email, string senhaHash, PerfilUsuario perfil)
            : base()
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do usuário é obrigatório.", nameof(nome));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("E-mail é obrigatório.", nameof(email));

            if (string.IsNullOrWhiteSpace(senhaHash))
                throw new ArgumentException("Senha é obrigatória.", nameof(senhaHash));

            Nome = nome;
            Email = email.Trim().ToLowerInvariant();
            SenhaHash = senhaHash;
            Perfil = perfil;
            Ativo = true;
        }

        public void AtualizarSenha(string novaSenhaHash)
        {
            if (string.IsNullOrWhiteSpace(novaSenhaHash))
                throw new ArgumentException("Nova senha é obrigatória.", nameof(novaSenhaHash));

            SenhaHash = novaSenhaHash;
            UpdateModificationDate();
        }

        public void Desativar()
        {
            Ativo = false;
            UpdateModificationDate();
        }

        public void Ativar()
        {
            Ativo = true;
            UpdateModificationDate();
        }
    }
}
