using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa um cliente da oficina
    /// </summary>
    public class Cliente : Entity
    {
        public string Nome { get; private set; }
        public string Documento { get; private set; } // CPF ou CNPJ (somente dígitos, normalizado)
        public string? Email { get; private set; }
        public string? Telefone { get; private set; }

        private Cliente() { }

        public Cliente(string nome, string documento, string? email = null, string? telefone = null)
            : base()
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do cliente e obrigatorio.", nameof(nome));

            // Valida CPF/CNPJ via Value Object — lança ArgumentException se inválido
            var documentoValidado = ValueObjects.Documento.Criar(documento);

            Nome = nome;
            Documento = documentoValidado.Valor;
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
