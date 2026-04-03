using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.Enums;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.UseCases.WorkOrders
{
    public class ChangeWorkOrderStatusUseCase
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly ILogger<ChangeWorkOrderStatusUseCase> _logger;

        public ChangeWorkOrderStatusUseCase(
            IWorkOrderRepository workOrderRepository,
            ILogger<ChangeWorkOrderStatusUseCase> logger)
        {
            _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChangeWorkOrderStatusResponse> HandleAsync(
            ChangeWorkOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Changing work order status: {WorkOrderId}, Action: {Action}", 
                request.WorkOrderId, request.Action);

            // Validate request
            Validate(request);

            // Get work order
            var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
            if (workOrder == null)
            {
                _logger.LogWarning("Work order not found: {WorkOrderId}", request.WorkOrderId);
                throw new NotFoundException("WorkOrder", request.WorkOrderId);
            }

            // Execute action (delegate to domain)
            try
            {
                ExecuteAction(workOrder, request.Action);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot execute action {Action} on work order {WorkOrderId}: {Message}", 
                    request.Action, request.WorkOrderId, ex.Message);
                throw new ValidationException("WorkOrder", ex.Message);
            }

            // Persist changes
            await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);

            _logger.LogInformation("Work order status changed successfully: {WorkOrderId}, Action: {Action}", 
                request.WorkOrderId, request.Action);

            return new ChangeWorkOrderStatusResponse(workOrder.Id);
        }

        private static void Validate(ChangeWorkOrderStatusRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.WorkOrderId == Guid.Empty)
            {
                errors.Add(nameof(request.WorkOrderId), new[] { "Work order ID is required." });
            }

            if (!Enum.IsDefined(typeof(WorkOrderAction), request.Action))
            {
                errors.Add(nameof(request.Action), new[] { "Invalid action." });
            }

            if (errors.Any())
            {
                throw new ValidationException(errors);
            }
        }

        private void ExecuteAction(WorkOrder workOrder, WorkOrderAction action)
        {
            // Delegate to domain methods - NO business logic here
            switch (action)
            {
                case WorkOrderAction.StartDiagnosis:
                    workOrder.StartDiagnosis();
                    break;

                case WorkOrderAction.RequestApproval:
                    workOrder.RequestApproval();
                    break;

                case WorkOrderAction.Approve:
                    workOrder.Approve();
                    break;

                case WorkOrderAction.Finish:
                    workOrder.Finish();
                    break;

                case WorkOrderAction.Deliver:
                    workOrder.Deliver();
                    break;

                case WorkOrderAction.Cancel:
                    workOrder.Cancel();
                    break;

                default:
                    // This should never happen due to validation, but kept for completeness
                    throw new InvalidOperationException($"Unsupported action: {action}");
            }
        }
    }
}
