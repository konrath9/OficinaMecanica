namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AddServiceToWorkOrderResponse
    {
        public Guid WorkOrderId { get; set; }

        public AddServiceToWorkOrderResponse(Guid workOrderId)
        {
            WorkOrderId = workOrderId;
        }
    }
}
