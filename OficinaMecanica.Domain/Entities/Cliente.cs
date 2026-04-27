using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Representa um cliente da oficina
    /// </summary>
    public class Cliente : Entity
    {
        private static readonly Regex RegexEmail =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            ValidarEmail(email);
            ValidarTelefone(telefone);

            Nome = nome;
            Documento = documentoValidado.Valor;
            Email = email?.Trim().ToLowerInvariant();
            Telefone = NormalizarTelefone(telefone);
        }

        public void Atualizar(string nome, string? email, string? telefone)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome do cliente e obrigatorio.", nameof(nome));

            ValidarEmail(email);
            ValidarTelefone(telefone);

            Nome = nome;
            Email = email?.Trim().ToLowerInvariant();
            Telefone = NormalizarTelefone(telefone);
            UpdateModificationDate();
        }

        public void AtualizarDocumento(string documento)
        {
            var documentoValidado = ValueObjects.Documento.Criar(documento);
            Documento = documentoValidado.Valor;
            UpdateModificationDate();
        }

        private static void ValidarEmail(string? email)
        {
            if (email is not null && !RegexEmail.IsMatch(email.Trim()))
                throw new ArgumentException(
                    $"E-mail inválido: '{email}'. Formato esperado: usuario@dominio.com", nameof(email));
        }

        private static void ValidarTelefone(string? telefone)
        {
            if (telefone is null) return;
            var digitos = Regex.Replace(telefone, @"\D", "");
            if (digitos.Length < 10 || digitos.Length > 11)
                throw new ArgumentException(
                    $"Telefone inválido: '{telefone}'. Informe DDD + número (10 ou 11 dígitos).", nameof(telefone));
        }

        private static string? NormalizarTelefone(string? telefone)
        {
            if (telefone is null) return null;
            return Regex.Replace(telefone, @"\D", "");
        }
    }
}
