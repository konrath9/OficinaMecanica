using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Represents a vehicle in the workshop
    /// </summary>
    public class Vehicle : Entity
    {
        public string LicensePlate { get; private set; }
        public string Brand { get; private set; }
        public string Model { get; private set; }
        public int Year { get; private set; }
        public Guid CustomerId { get; private set; }

        // Navigation property
        public Customer? Customer { get; private set; }

        private Vehicle() { }

        public Vehicle(string licensePlate, string brand, string model, int year, Guid customerId)
            : base()
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                throw new ArgumentException("License plate is required.", nameof(licensePlate));

            if (string.IsNullOrWhiteSpace(brand))
                throw new ArgumentException("Brand is required.", nameof(brand));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));

            if (year < 1900 || year > DateTime.UtcNow.Year + 1)
                throw new ArgumentException("Invalid year.", nameof(year));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer is required.", nameof(customerId));

            LicensePlate = licensePlate.ToUpperInvariant();
            Brand = brand;
            Model = model;
            Year = year;
            CustomerId = customerId;
        }

        public void Update(string brand, string model, int year)
        {
            if (string.IsNullOrWhiteSpace(brand))
                throw new ArgumentException("Brand is required.", nameof(brand));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));

            if (year < 1900 || year > DateTime.UtcNow.Year + 1)
                throw new ArgumentException("Invalid year.", nameof(year));

            Brand = brand;
            Model = model;
            Year = year;
            UpdateModificationDate();
        }
    }
}
