namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Represents a service item in a work order (references the Service catalog)
    /// </summary>
    public class ServiceItem
    {
        public Guid ServiceId { get; private set; }
        public string Description { get; private set; } // Snapshot of service name at the moment
        public decimal UnitPrice { get; private set; } // Price frozen at the moment of work order creation
        public int Quantity { get; private set; }
        public decimal TotalPrice => UnitPrice * Quantity;

        private ServiceItem() { }

        public ServiceItem(Guid serviceId, string description, decimal unitPrice, int quantity)
        {
            if (serviceId == Guid.Empty)
                throw new ArgumentException("Service ID is required.", nameof(serviceId));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Service description is required.", nameof(description));

            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            ServiceId = serviceId;
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
