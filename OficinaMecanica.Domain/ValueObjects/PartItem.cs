namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Represents a part item in a work order (references the Part catalog)
    /// </summary>
    public class PartItem
    {
        public Guid PartId { get; private set; }
        public string Code { get; private set; } // Snapshot of part code at the moment
        public string Description { get; private set; } // Snapshot of part name at the moment
        public decimal UnitPrice { get; private set; } // Price frozen at the moment of work order creation
        public int Quantity { get; private set; }
        public decimal TotalPrice => UnitPrice * Quantity;

        private PartItem() { }

        public PartItem(Guid partId, string code, string description, decimal unitPrice, int quantity)
        {
            if (partId == Guid.Empty)
                throw new ArgumentException("Part ID is required.", nameof(partId));

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Part code is required.", nameof(code));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Part description is required.", nameof(description));

            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            PartId = partId;
            Code = code;
            Description = description;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

            Quantity = newQuantity;
        }

        public void UpdateUnitPrice(decimal newPrice)
        {
            if (newPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero.", nameof(newPrice));

            UnitPrice = newPrice;
        }
    }
}
