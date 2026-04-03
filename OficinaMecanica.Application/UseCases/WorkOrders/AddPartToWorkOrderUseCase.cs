using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public class AddPartToWorkOrderUseCase
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly IPartRepository _partRepository;
        private readonly ILogger<AddPartToWorkOrderUseCase> _logger;

        public AddPartToWorkOrderUseCase(
            IWorkOrderRepository workOrderRepository,
            IPartRepository partRepository,
            ILogger<AddPartToWorkOrderUseCase> logger)
        {
            _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
            _partRepository = partRepository ?? throw new ArgumentNullException(nameof(partRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AddPartToWorkOrderResponse> HandleAsync(
            AddPartToWorkOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Adding part to work order: {WorkOrderId}", request.WorkOrderId);

            // Validate request
            Validate(request);

            // Get work order
            var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
            if (workOrder == null)
            {
                _logger.LogWarning("Work order not found: {WorkOrderId}", request.WorkOrderId);
                throw new NotFoundException("WorkOrder", request.WorkOrderId);
            }

            // Get part from catalog
            var part = await _partRepository.GetByIdAsync(request.PartId, cancellationToken);
            if (part == null)
            {
                _logger.LogWarning("Part not found: {PartId}", request.PartId);
                throw new NotFoundException("Part", request.PartId);
            }

            // Check stock availability
            if (!part.HasStock(request.Quantity))
            {
                _logger.LogWarning("Insufficient stock for part {PartId}. Required: {Required}, Available: {Available}",
                    request.PartId, request.Quantity, part.StockQuantity);
                throw new ValidationException("Part", $"Insufficient stock. Available: {part.StockQuantity}");
            }

            // Create part item with catalog data (price snapshot)
            PartItem partItem;
            try
            {
                partItem = new PartItem(
                    partId: part.Id,
                    code: part.Code,
                    description: part.Name,
                    unitPrice: part.UnitPrice,
                    quantity: request.Quantity
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid part item for work order {WorkOrderId}: {Message}", 
                    request.WorkOrderId, ex.Message);
                throw new ValidationException("PartItem", ex.Message);
            }

            // Add part to work order (domain enforces editability)
            try
            {
                workOrder.AddPart(partItem);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot add part to work order {WorkOrderId}: {Message}", 
                    request.WorkOrderId, ex.Message);
                throw new ValidationException("WorkOrder", ex.Message);
            }

            // Reserve stock (decrease available quantity)
            part.RemoveStock(request.Quantity);
            await _partRepository.UpdateAsync(part, cancellationToken);

            // Persist work order changes
            await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);

            _logger.LogInformation("Part added successfully to work order: {WorkOrderId}", request.WorkOrderId);

            return new AddPartToWorkOrderResponse(workOrder.Id);
        }

        private static void Validate(AddPartToWorkOrderRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.WorkOrderId == Guid.Empty)
            {
                errors.Add(nameof(request.WorkOrderId), new[] { "Work order ID is required." });
            }

            if (request.PartId == Guid.Empty)
            {
                errors.Add(nameof(request.PartId), new[] { "Part ID is required." });
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
