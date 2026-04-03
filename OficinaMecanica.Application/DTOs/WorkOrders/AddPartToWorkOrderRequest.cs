namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AddPartToWorkOrderRequest
    {
        public Guid WorkOrderId { get; set; }
        public Guid PartId { get; set; }
        public int Quantity { get; set; }
    }
}

