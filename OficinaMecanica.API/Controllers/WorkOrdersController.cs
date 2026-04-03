using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Application.Common.Exceptions;
using OficinaMecanica.Application.DTOs.WorkOrders;
using OficinaMecanica.Application.UseCases.WorkOrders;

namespace OficinaMecanica.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkOrdersController : ControllerBase
    {
        private readonly CreateWorkOrderUseCase _createWorkOrderUseCase;
        private readonly AddServiceToWorkOrderUseCase _addServiceToWorkOrderUseCase;
        private readonly AddPartToWorkOrderUseCase _addPartToWorkOrderUseCase;
        private readonly ChangeWorkOrderStatusUseCase _changeWorkOrderStatusUseCase;
        private readonly ILogger<WorkOrdersController> _logger;

        public WorkOrdersController(
            CreateWorkOrderUseCase createWorkOrderUseCase,
            AddServiceToWorkOrderUseCase addServiceToWorkOrderUseCase,
            AddPartToWorkOrderUseCase addPartToWorkOrderUseCase,
            ChangeWorkOrderStatusUseCase changeWorkOrderStatusUseCase,
            ILogger<WorkOrdersController> logger)
        {
            _createWorkOrderUseCase = createWorkOrderUseCase;
            _addServiceToWorkOrderUseCase = addServiceToWorkOrderUseCase;
            _addPartToWorkOrderUseCase = addPartToWorkOrderUseCase;
            _changeWorkOrderStatusUseCase = changeWorkOrderStatusUseCase;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkOrder(
            [FromBody] CreateWorkOrderRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _createWorkOrderUseCase.HandleAsync(request, cancellationToken);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = response.Id },
                    response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work order");
                return StatusCode(500, new { message = "An error occurred while creating the work order" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            // TODO: Implement GetWorkOrderByIdUseCase
            return Ok();
        }

        [HttpPost("{id}/services")]
        public async Task<IActionResult> AddService(
            Guid id,
            [FromBody] AddServiceToWorkOrderRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.WorkOrderId = id;
                var response = await _addServiceToWorkOrderUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding service to work order {WorkOrderId}", id);
                return StatusCode(500, new { message = "An error occurred while adding the service" });
            }
        }

        [HttpPost("{id}/parts")]
        public async Task<IActionResult> AddPart(
            Guid id,
            [FromBody] AddPartToWorkOrderRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.WorkOrderId = id;
                var response = await _addPartToWorkOrderUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding part to work order {WorkOrderId}", id);
                return StatusCode(500, new { message = "An error occurred while adding the part" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromBody] ChangeWorkOrderStatusRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.WorkOrderId = id;
                var response = await _changeWorkOrderStatusUseCase.HandleAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing work order status {WorkOrderId}", id);
                return StatusCode(500, new { message = "An error occurred while changing the status" });
            }
        }
    }
}
