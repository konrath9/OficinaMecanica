# AddServiceToWorkOrder Use Case

## ?? Overview
This use case handles adding a service item to an existing work order, enforcing business rules through the domain layer.

---

## ?? Use Case: AddServiceToWorkOrderUseCase

### Responsibility
Adds a service to an existing work order while respecting domain invariants and business rules.

### Dependencies
```csharp
public AddServiceToWorkOrderUseCase(
    IWorkOrderRepository workOrderRepository,
    ILogger<AddServiceToWorkOrderUseCase> logger)
```

---

## ?? Input (Request DTO)

```csharp
public class AddServiceToWorkOrderRequest
{
    public Guid WorkOrderId { get; set; }      // Required - Work order to add service to
    public string Description { get; set; }     // Required - Service description
    public decimal UnitPrice { get; set; }      // Required - Must be > 0
    public int Quantity { get; set; }           // Required - Must be > 0
}
```

### Example Request
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Engine oil change",
  "unitPrice": 89.90,
  "quantity": 1
}
```

---

## ?? Output (Response DTO)

```csharp
public class AddServiceToWorkOrderResponse
{
    public Guid WorkOrderId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}
```

### Success Response
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "success": true,
  "message": "Service added successfully to work order."
}
```

---

## ?? Execution Flow

### Sequence Diagram
```
Controller ? UseCase ? Validate ? Get WorkOrder ? Create ServiceItem ? Domain.AddService() ? Persist ? Response
```

### Detailed Steps

#### 1. **Request Validation**
```csharp
ValidateRequest(request);
```

**Validations:**
- ? WorkOrderId is not empty
- ? Description is not empty or whitespace
- ? UnitPrice > 0
- ? Quantity > 0

**Throws:** `ValidationException` if validation fails

---

#### 2. **Get Work Order**
```csharp
var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
if (workOrder == null)
    throw new NotFoundException("WorkOrder", request.WorkOrderId);
```

**Throws:** `NotFoundException` if work order doesn't exist

**Why This Matters:**
- Ensures work order exists before attempting to modify it
- Provides clear error message to the user

---

#### 3. **Create Service Item (Value Object)**
```csharp
var serviceItem = new ServiceItem(
    description: request.Description,
    unitPrice: request.UnitPrice,
    quantity: request.Quantity
);
```

**Domain Validation:**
- ServiceItem constructor validates:
  - Description is not empty
  - UnitPrice > 0
  - Quantity > 0

**Throws:** `ValidationException` if domain validation fails

---

#### 4. **Add Service to Work Order (Domain Method)**
```csharp
workOrder.AddService(serviceItem);
```

**Business Rules Enforced by Domain:**
```csharp
// Inside WorkOrder.AddService()
private void ValidateOrderIsEditable()
{
    if (Status == WorkOrderStatus.Finished || 
        Status == WorkOrderStatus.Delivered || 
        Status == WorkOrderStatus.Cancelled)
    {
        throw new InvalidOperationException("Cannot edit a finished, delivered or cancelled order.");
    }
}
```

**Valid Statuses for Adding Service:**
- ? Received
- ? InDiagnosis
- ? WaitingForApproval
- ? InExecution

**Invalid Statuses:**
- ? Finished
- ? Delivered
- ? Cancelled

**Throws:** `ValidationException` wrapping `InvalidOperationException`

---

#### 5. **Persist Changes**
```csharp
await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
```

**Repository Updates:**
- Work order entity with new service
- Updated `UpdatedAt` timestamp
- Recalculated `TotalPrice`

---

#### 6. **Return Response**
```csharp
return AddServiceToWorkOrderResponse.CreateSuccess(workOrder.Id);
```

---

## ?? Business Rules Enforced

### 1. **Work Order Must Exist**
```csharp
if (workOrder == null)
    throw new NotFoundException("WorkOrder", request.WorkOrderId);
```

---

### 2. **Work Order Must Be Editable**
```csharp
// Enforced by domain: workOrder.AddService()
// Cannot add services to:
// - Finished orders
// - Delivered orders
// - Cancelled orders
```

**Status Validation:**
| Status | Can Add Service? |
|--------|------------------|
| Received | ? Yes |
| InDiagnosis | ? Yes |
| WaitingForApproval | ? Yes |
| InExecution | ? Yes |
| Finished | ? No |
| Delivered | ? No |
| Cancelled | ? No |

---

### 3. **Service Item Must Be Valid**
```csharp
// Enforced by ServiceItem constructor
- Description: Required (not empty/whitespace)
- UnitPrice: Must be > 0
- Quantity: Must be > 0
```

---

## ?? Exception Handling

### ValidationException (400 Bad Request)
**Scenario 1:** Invalid request data
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "Description": ["Service description is required."],
    "UnitPrice": ["Unit price must be greater than zero."]
  }
}
```

**Scenario 2:** Domain validation failure
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "ServiceItem": ["Service description is required."]
  }
}
```

**Scenario 3:** Business rule violation
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "WorkOrder": ["Cannot edit a finished, delivered or cancelled order."]
  }
}
```

---

### NotFoundException (404 Not Found)
```json
{
  "type": "NotFoundException",
  "title": "Entity \"WorkOrder\" (3fa85f64-5717-4562-b3fc-2c963f66afa6) was not found."
}
```

---

## ?? Logging

### Log Levels

#### Information
```csharp
_logger.LogInformation("Starting to add service to work order: {WorkOrderId}", request.WorkOrderId);
_logger.LogInformation("Service added successfully to work order: {WorkOrderId}", request.WorkOrderId);
```

#### Debug
```csharp
_logger.LogDebug("Work order found. Current status: {Status}", workOrder.Status);
_logger.LogDebug("Service item created: {Description}, Unit Price: {UnitPrice}, Quantity: {Quantity}", ...);
_logger.LogDebug("Service added to work order entity");
```

#### Warning
```csharp
_logger.LogWarning("Work order not found: {WorkOrderId}", request.WorkOrderId);
_logger.LogWarning(ex, "Failed to create service item. Validation error: {Message}", ex.Message);
_logger.LogWarning(ex, "Failed to add service to work order. Business rule violation: {Message}", ex.Message);
```

### Example Log Output
```
[2024-03-15 15:30:45] INFO  Starting to add service to work order: 3fa85f64-5717-4562-b3fc-2c963f66afa6
[2024-03-15 15:30:45] DEBUG Work order found. Current status: InDiagnosis
[2024-03-15 15:30:45] DEBUG Service item created: Engine oil change, Unit Price: 89.90, Quantity: 1
[2024-03-15 15:30:45] DEBUG Service added to work order entity
[2024-03-15 15:30:45] INFO  Service added successfully to work order: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## ?? Testing

### Unit Test Example

```csharp
public class AddServiceToWorkOrderUseCaseTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_AddsServiceSuccessfully()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var request = new AddServiceToWorkOrderRequest
        {
            WorkOrderId = workOrderId,
            Description = "Engine oil change",
            UnitPrice = 89.90m,
            Quantity = 1
        };

        var workOrder = new WorkOrder("WO-001", Guid.NewGuid(), Guid.NewGuid());
        
        var mockRepository = new Mock<IWorkOrderRepository>();
        mockRepository
            .Setup(x => x.GetByIdAsync(workOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workOrder);
        
        mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<AddServiceToWorkOrderUseCase>>();
        var useCase = new AddServiceToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act
        var response = await useCase.HandleAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(workOrderId, response.WorkOrderId);
        Assert.Equal(1, workOrder.Services.Count);
        mockRepository.Verify(x => x.UpdateAsync(workOrder, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WorkOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new AddServiceToWorkOrderRequest
        {
            WorkOrderId = Guid.NewGuid(),
            Description = "Engine oil change",
            UnitPrice = 89.90m,
            Quantity = 1
        };

        var mockRepository = new Mock<IWorkOrderRepository>();
        mockRepository
            .Setup(x => x.GetByIdAsync(request.WorkOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var mockLogger = new Mock<ILogger<AddServiceToWorkOrderUseCase>>();
        var useCase = new AddServiceToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_WorkOrderFinished_ThrowsValidationException()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var request = new AddServiceToWorkOrderRequest
        {
            WorkOrderId = workOrderId,
            Description = "Engine oil change",
            UnitPrice = 89.90m,
            Quantity = 1
        };

        var workOrder = new WorkOrder("WO-001", Guid.NewGuid(), Guid.NewGuid());
        workOrder.StartDiagnosis();
        workOrder.RequestApproval();
        workOrder.Approve();
        workOrder.Finish();  // Status = Finished (not editable)

        var mockRepository = new Mock<IWorkOrderRepository>();
        mockRepository
            .Setup(x => x.GetByIdAsync(workOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workOrder);

        var mockLogger = new Mock<ILogger<AddServiceToWorkOrderUseCase>>();
        var useCase = new AddServiceToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.HandleAsync(request));
        Assert.Contains("WorkOrder", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("", 89.90, 1, "Description")]
    [InlineData("Oil change", 0, 1, "UnitPrice")]
    [InlineData("Oil change", -10, 1, "UnitPrice")]
    [InlineData("Oil change", 89.90, 0, "Quantity")]
    [InlineData("Oil change", 89.90, -1, "Quantity")]
    public async Task HandleAsync_InvalidRequest_ThrowsValidationException(
        string description, decimal unitPrice, int quantity, string expectedErrorKey)
    {
        // Arrange
        var request = new AddServiceToWorkOrderRequest
        {
            WorkOrderId = Guid.NewGuid(),
            Description = description,
            UnitPrice = unitPrice,
            Quantity = quantity
        };

        var mockRepository = new Mock<IWorkOrderRepository>();
        var mockLogger = new Mock<ILogger<AddServiceToWorkOrderUseCase>>();
        var useCase = new AddServiceToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.HandleAsync(request));
        Assert.Contains(expectedErrorKey, exception.Errors.Keys);
    }
}
```

---

## ?? Usage Example (API Controller)

```csharp
[ApiController]
[Route("api/work-orders")]
public class WorkOrdersController : ControllerBase
{
    private readonly AddServiceToWorkOrderUseCase _addServiceUseCase;

    public WorkOrdersController(AddServiceToWorkOrderUseCase addServiceUseCase)
    {
        _addServiceUseCase = addServiceUseCase;
    }

    [HttpPost("{workOrderId}/services")]
    [ProducesResponseType(typeof(AddServiceToWorkOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddServiceToWorkOrderResponse>> AddService(
        Guid workOrderId,
        [FromBody] AddServiceToWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        // Override WorkOrderId from route
        request.WorkOrderId = workOrderId;

        try
        {
            var response = await _addServiceUseCase.HandleAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails 
            { 
                Title = "Work order not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = ex.Errors }
            });
        }
    }
}
```

### Example API Call
```bash
POST /api/work-orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/services
Content-Type: application/json

{
  "description": "Engine oil change",
  "unitPrice": 89.90,
  "quantity": 1
}
```

**Success Response (200 OK):**
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "success": true,
  "message": "Service added successfully to work order."
}
```

---

## ?? Design Principles Applied

### ? Single Responsibility Principle (SRP)
- Use case only handles adding service to work order
- Domain enforces business rules
- Repository handles persistence

### ? Dependency Inversion Principle (DIP)
- Depends on `IWorkOrderRepository` abstraction
- No direct dependencies on infrastructure

### ? Domain-Driven Design
- Business logic in domain layer (`workOrder.AddService()`)
- Domain entities enforce invariants
- Use case orchestrates the operation

### ? Error Handling
- Clear exception types for different scenarios
- Detailed error messages for debugging
- Proper HTTP status codes

### ? Observability
- Structured logging with contextual information
- Log levels appropriate for each event
- Traceable request flow

---

## ?? Key Takeaways

1. **Domain Protection:** Business rules are enforced by the domain, not the application layer
2. **Clear Validation:** Multiple layers of validation (request, domain, business rules)
3. **Proper Error Handling:** Specific exceptions for different scenarios
4. **Logging:** Complete observability of the operation
5. **Testability:** Easy to unit test with mocked dependencies

---

**This use case follows production-ready standards and best practices!** ??
