using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Represents a service offered by the workshop
    /// </summary>
    public class Service : Entity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }

        private Service() { }

        public Service(string name, string description, decimal price)
            : base()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Service name is required.", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Service description is required.", nameof(description));

            if (price < 0)
                throw new ArgumentException("Service price cannot be negative.", nameof(price));

            Name = name;
            Description = description;
            Price = price;
        }

        public void Update(string name, string description, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Service name is required.", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Service description is required.", nameof(description));

            if (price < 0)
                throw new ArgumentException("Service price cannot be negative.", nameof(price));

            Name = name;
            Description = description;
            Price = price;
            UpdateModificationDate();
        }
    }
}
