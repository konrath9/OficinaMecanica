using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Represents a customer in the workshop
    /// </summary>
    public class Customer : Entity
    {
        public string Name { get; private set; }
        public string Document { get; private set; } // CPF or CNPJ
        public string? Email { get; private set; }
        public string? Phone { get; private set; }

        private Customer() { }

        public Customer(string name, string document, string? email = null, string? phone = null)
            : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name is required.", nameof(name));

            if (string.IsNullOrWhiteSpace(document))
                throw new ArgumentException("Document (CPF/CNPJ) is required.", nameof(document));

            Name = name;
            Document = document;
            Email = email;
            Phone = phone;
        }

        public void Update(string name, string? email, string? phone)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name is required.", nameof(name));

            Name = name;
            Email = email;
            Phone = phone;
            UpdateModificationDate();
        }
    }
}
