using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public class AddServiceToWorkOrderUseCase
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ILogger<AddServiceToWorkOrderUseCase> _logger;

        public AddServiceToWorkOrderUseCase(
            IWorkOrderRepository workOrderRepository,
            IServiceRepository serviceRepository,
            ILogger<AddServiceToWorkOrderUseCase> logger)
        {
            _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
            _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AddServiceToWorkOrderResponse> HandleAsync(
            AddServiceToWorkOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Adding service to work order: {WorkOrderId}", request.WorkOrderId);

            // Validate request
            Validate(request);

            // Get work order
            var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
            if (workOrder == null)
            {
                _logger.LogWarning("Work order not found: {WorkOrderId}", request.WorkOrderId);
                throw new NotFoundException("WorkOrder", request.WorkOrderId);
            }

            // Get service from catalog
            var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
            if (service == null)
            {
                _logger.LogWarning("Service not found: {ServiceId}", request.ServiceId);
                throw new NotFoundException("Service", request.ServiceId);
            }

            // Create service item with catalog data (price snapshot)
            ServiceItem serviceItem;
            try
            {
                serviceItem = new ServiceItem(
                    serviceId: service.Id,
                    description: service.Name,
                    unitPrice: service.Price,
                    quantity: request.Quantity
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid service item for work order {WorkOrderId}: {Message}", 
                    request.WorkOrderId, ex.Message);
                throw new ValidationException("ServiceItem", ex.Message);
            }

            // Add service to work order (domain enforces editability)
            try
            {
                workOrder.AddService(serviceItem);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot add service to work order {WorkOrderId}: {Message}", 
                    request.WorkOrderId, ex.Message);
                throw new ValidationException("WorkOrder", ex.Message);
            }

            // Persist changes
            await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);

            _logger.LogInformation("Service added successfully to work order: {WorkOrderId}", request.WorkOrderId);

            return new AddServiceToWorkOrderResponse(workOrder.Id);
        }

        private static void Validate(AddServiceToWorkOrderRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.WorkOrderId == Guid.Empty)
            {
                errors.Add(nameof(request.WorkOrderId), new[] { "Work order ID is required." });
            }

            if (request.ServiceId == Guid.Empty)
            {
                errors.Add(nameof(request.ServiceId), new[] { "Service ID is required." });
            }

            if (request.Quantity <= 0)
            {
                errors.Add(nameof(request.Quantity), new[] { "Quantity must be greater than zero." });
            }

            if (errors.Any())
            {
                throw new ValidationException(errors);
            }
        }
    }
}
