using OficinaMecanica.Domain.Common;

namespace OficinaMecanica.Domain.Entities
{
    /// <summary>
    /// Represents a part/component in the workshop with stock control
    /// </summary>
    public class Part : Entity
    {
        public string Code { get; private set; }
        public string Name { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int StockQuantity { get; private set; }

        private Part() { }

        public Part(string code, string name, decimal unitPrice, int stockQuantity = 0)
            : base()
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Part code is required.", nameof(code));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Part name is required.", nameof(name));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

            Code = code.ToUpperInvariant();
            Name = name;
            UnitPrice = unitPrice;
            StockQuantity = stockQuantity;
        }

        public void Update(string name, decimal unitPrice)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Part name is required.", nameof(name));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

            Name = name;
            UnitPrice = unitPrice;
            UpdateModificationDate();
        }

        public void AddStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            StockQuantity += quantity;
            UpdateModificationDate();
        }

        public void RemoveStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (quantity > StockQuantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {StockQuantity}");

            StockQuantity -= quantity;
            UpdateModificationDate();
        }

        public bool HasStock(int quantity) => StockQuantity >= quantity;
    }
}
