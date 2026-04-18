using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa um cliente da oficina
    /// </summary>
    public class Cliente : Entity
    {
        public string Nome { get; private set; }
        public string Documento { get; private set; } // CPF ou CNPJ
        public string? Email { get; private set; }
        public string? Telefone { get; private set; }

        private Cliente() { }

        public Cliente(string nome, string documento, string? email = null, string? telefone = null)
            : base()
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do cliente e obrigatorio.", nameof(nome));

            if (string.IsNullOrWhiteSpace(documento))
                throw new ArgumentException("Documento (CPF/CNPJ) e obrigatorio.", nameof(documento));

            Nome = nome;
            Documento = documento;
            Email = email;
            Telefone = telefone;
        }

        public void Atualizar(string nome, string? email, string? telefone)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do cliente e obrigatorio.", nameof(nome));

            Nome = nome;
            Email = email;
            Telefone = telefone;
            UpdateModificationDate();
        }
    }
}
