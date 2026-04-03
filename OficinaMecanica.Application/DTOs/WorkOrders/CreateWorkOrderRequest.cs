namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class CreateWorkOrderRequest
    {
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public string? Notes { get; set; }
    }
}
