using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class CreateWorkOrderResponse
    {
        public Guid Id { get; set; }
        public string Number { get; set; }
        public WorkOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public CreateWorkOrderResponse(Guid id, string number, WorkOrderStatus status, DateTime createdAt)
        {
            Id = id;
            Number = number;
            Status = status;
            CreatedAt = createdAt;
        }
    }
}
