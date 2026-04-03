using OficinaMecanica.Application.Enums;

namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class ChangeWorkOrderStatusRequest
    {
        public Guid WorkOrderId { get; set; }
        public WorkOrderAction Action { get; set; }
    }
}
