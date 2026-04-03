using OficinaMecanica.Domain.Common;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Domain.Entities
{
    public class WorkOrder : Entity
    {
        private readonly List<ServiceItem> _services;
        private readonly List<PartItem> _parts;

        public string Number { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid VehicleId { get; private set; }
        public WorkOrderStatus Status { get; private set; }
        public string? Notes { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }
        public DateTime? DeliveredAt { get; private set; }
        public IReadOnlyCollection<ServiceItem> Services => _services.AsReadOnly();
        public IReadOnlyCollection<PartItem> Parts => _parts.AsReadOnly();

        public decimal TotalServicesPrice => _services.Sum(s => s.TotalPrice);
        public decimal TotalPartsPrice => _parts.Sum(p => p.TotalPrice);
        public decimal TotalPrice => TotalServicesPrice + TotalPartsPrice;

        private WorkOrder() 
        {
            _services = new List<ServiceItem>();
            _parts = new List<PartItem>();
        }

        public WorkOrder(string number, Guid customerId, Guid vehicleId, string? notes = null)
            : base()
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new ArgumentException("Work order number is required.", nameof(number));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer is required.", nameof(customerId));

            if (vehicleId == Guid.Empty)
                throw new ArgumentException("Vehicle is required.", nameof(vehicleId));

            Number = number;
            CustomerId = customerId;
            VehicleId = vehicleId;
            Status = WorkOrderStatus.Received;
            Notes = notes;
            _services = new List<ServiceItem>();
            _parts = new List<PartItem>();
        }

        public void AddService(ServiceItem service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            ValidateOrderIsEditable();

            _services.Add(service);
            UpdateModificationDate();
        }

        public void RemoveService(string description)
        {
            ValidateOrderIsEditable();

            var service = _services.FirstOrDefault(s => s.Description == description);
            if (service != null)
            {
                _services.Remove(service);
                UpdateModificationDate();
            }
        }

        public void AddPart(PartItem part)
        {
            if (part == null)
                throw new ArgumentNullException(nameof(part));

            ValidateOrderIsEditable();

            _parts.Add(part);
            UpdateModificationDate();
        }

        public void RemovePart(string code)
        {
            ValidateOrderIsEditable();

            var part = _parts.FirstOrDefault(p => p.Code == code);
            if (part != null)
            {
                _parts.Remove(part);
                UpdateModificationDate();
            }
        }

        public void StartDiagnosis()
        {
            if (Status != WorkOrderStatus.Received)
                throw new InvalidOperationException("Only received orders can start diagnosis.");

            Status = WorkOrderStatus.InDiagnosis;
            StartedAt = DateTime.UtcNow;
            UpdateModificationDate();
        }

        public void RequestApproval()
        {
            if (Status != WorkOrderStatus.InDiagnosis)
                throw new InvalidOperationException("Only orders in diagnosis can request approval.");

            if (!_services.Any() && !_parts.Any())
                throw new InvalidOperationException("The order must have at least one service or part to request approval.");

            Status = WorkOrderStatus.WaitingForApproval;
            UpdateModificationDate();
        }

        public void Approve()
        {
            if (Status != WorkOrderStatus.WaitingForApproval)
                throw new InvalidOperationException("Only orders waiting for approval can be approved.");

            Status = WorkOrderStatus.InExecution;
            if (StartedAt == null)
                StartedAt = DateTime.UtcNow;
            UpdateModificationDate();
        }

        public void Finish()
        {
            if (Status != WorkOrderStatus.InExecution)
                throw new InvalidOperationException("Only orders in execution can be finished.");

            if (!_services.Any() && !_parts.Any())
                throw new InvalidOperationException("The order must have at least one service or part to be finished.");

            Status = WorkOrderStatus.Finished;
            FinishedAt = DateTime.UtcNow;
            UpdateModificationDate();
        }

        public void Deliver()
        {
            if (Status != WorkOrderStatus.Finished)
                throw new InvalidOperationException("Only finished orders can be delivered.");

            Status = WorkOrderStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
            UpdateModificationDate();
        }

        public void Cancel(string? reason = null)
        {
            if (Status == WorkOrderStatus.Finished || Status == WorkOrderStatus.Delivered)
                throw new InvalidOperationException("Finished or delivered orders cannot be cancelled.");

            Status = WorkOrderStatus.Cancelled;
            if (!string.IsNullOrWhiteSpace(reason))
                Notes = $"{Notes}\nCancellation reason: {reason}";
            UpdateModificationDate();
        }

        public void UpdateNotes(string notes)
        {
            Notes = notes;
            UpdateModificationDate();
        }

        private void ValidateOrderIsEditable()
        {
            if (Status == WorkOrderStatus.Finished || 
                Status == WorkOrderStatus.Delivered || 
                Status == WorkOrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot edit a finished, delivered or cancelled order.");
            }
        }
    }
}
