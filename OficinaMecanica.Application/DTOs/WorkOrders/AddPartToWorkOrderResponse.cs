namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AddPartToWorkOrderResponse
    {
        public Guid WorkOrderId { get; set; }

        public AddPartToWorkOrderResponse(Guid workOrderId)
        {
            WorkOrderId = workOrderId;
        }
    }
}
