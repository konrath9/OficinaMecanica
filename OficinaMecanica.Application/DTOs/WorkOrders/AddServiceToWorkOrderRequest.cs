namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AddServiceToWorkOrderRequest
    {
        public Guid WorkOrderId { get; set; }
        public Guid ServiceId { get; set; }
        public int Quantity { get; set; }
    }
}

