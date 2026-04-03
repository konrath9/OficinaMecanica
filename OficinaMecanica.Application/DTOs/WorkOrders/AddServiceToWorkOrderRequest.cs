namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AddServiceToWorkOrderRequest
    {
        public Guid WorkOrderId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
