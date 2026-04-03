namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AddPartToWorkOrderRequest
    {
        public Guid WorkOrderId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
