using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public class CreateWorkOrderUseCase
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ILogger<CreateWorkOrderUseCase> _logger;

        public CreateWorkOrderUseCase(
            IWorkOrderRepository workOrderRepository,
            ICustomerRepository customerRepository,
            IVehicleRepository vehicleRepository,
            ILogger<CreateWorkOrderUseCase> logger)
        {
            _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _vehicleRepository = vehicleRepository ?? throw new ArgumentNullException(nameof(vehicleRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreateWorkOrderResponse> HandleAsync(
            CreateWorkOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting work order creation for Customer: {CustomerId}, Vehicle: {VehicleId}", 
                request.CustomerId, request.VehicleId);

            // Validate request
            await ValidateRequestAsync(request, cancellationToken);

            // Generate work order number
            var workOrderNumber = GenerateWorkOrderNumber();

            // Create work order entity
            WorkOrder workOrder;
            try
            {
                workOrder = new WorkOrder(
                    number: workOrderNumber,
                    customerId: request.CustomerId,
                    vehicleId: request.VehicleId,
                    notes: request.Notes
                );

                _logger.LogDebug("Work order entity created with number: {WorkOrderNumber}", workOrderNumber);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Failed to create work order entity. Validation error: {Message}", ex.Message);
                throw new ValidationException("WorkOrder", ex.Message);
            }

            // Persist work order
            var createdWorkOrder = await _workOrderRepository.AddAsync(workOrder, cancellationToken);

            _logger.LogInformation("Work order created successfully. Id: {WorkOrderId}, Number: {WorkOrderNumber}", 
                createdWorkOrder.Id, createdWorkOrder.Number);

            // Return response
            return new CreateWorkOrderResponse(
                id: createdWorkOrder.Id,
                number: createdWorkOrder.Number,
                status: createdWorkOrder.Status,
                createdAt: createdWorkOrder.CreatedAt
            );
        }

        private async Task ValidateRequestAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.CustomerId == Guid.Empty)
            {
                errors.Add(nameof(request.CustomerId), new[] { "Customer ID is required." });
            }
            else
            {
                // Validate customer exists
                var customerExists = await _customerRepository.ExistsAsync(request.CustomerId, cancellationToken);
                if (!customerExists)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", request.CustomerId);
                    throw new NotFoundException("Customer", request.CustomerId);
                }
            }

            if (request.VehicleId == Guid.Empty)
            {
                errors.Add(nameof(request.VehicleId), new[] { "Vehicle ID is required." });
            }
            else
            {
                // Validate vehicle exists
                var vehicleExists = await _vehicleRepository.ExistsAsync(request.VehicleId, cancellationToken);
                if (!vehicleExists)
                {
                    _logger.LogWarning("Vehicle not found: {VehicleId}", request.VehicleId);
                    throw new NotFoundException("Vehicle", request.VehicleId);
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("Validation failed for work order creation: {Errors}", errors);
                throw new ValidationException(errors);
            }
        }

        private static string GenerateWorkOrderNumber()
        {
            // Simple number generation for MVP
            // Format: WO-{timestamp}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"WO-{timestamp}";
        }
    }
}
