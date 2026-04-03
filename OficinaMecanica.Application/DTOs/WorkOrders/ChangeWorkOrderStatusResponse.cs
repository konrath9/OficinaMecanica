namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class ChangeWorkOrderStatusResponse
    {
        public Guid WorkOrderId { get; set; }

        public ChangeWorkOrderStatusResponse(Guid workOrderId)
        {
            WorkOrderId = workOrderId;
        }
    }
}
