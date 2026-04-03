# AddPartToWorkOrder Use Case

## ?? Overview
This use case handles adding a part (spare part) to an existing work order, following the same refined pattern as `AddServiceToWorkOrder`.

---

## ?? Use Case: AddPartToWorkOrderUseCase

### Responsibility
Adds a part item to an existing work order while respecting domain invariants and business rules.

### Dependencies
```csharp
public AddPartToWorkOrderUseCase(
    IWorkOrderRepository workOrderRepository,
    ILogger<AddPartToWorkOrderUseCase> logger)
```

---

## ?? Input (Request DTO)

```csharp
public class AddPartToWorkOrderRequest
{
    public Guid WorkOrderId { get; set; }      // Required - Work order to add part to
    public string Code { get; set; }           // Required - Part code/SKU
    public string Description { get; set; }    // Required - Part description
    public decimal UnitPrice { get; set; }     // Required - Must be > 0 (validated by domain)
    public int Quantity { get; set; }          // Required - Must be > 0 (validated by domain)
}
```

### Example Request
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "FILTER-OIL-001",
  "description": "Engine oil filter",
  "unitPrice": 45.90,
  "quantity": 1
}
```

---

## ?? Output (Response DTO)

```csharp
public class AddPartToWorkOrderResponse
{
    public Guid WorkOrderId { get; set; }
    
    public AddPartToWorkOrderResponse(Guid workOrderId)
    {
        WorkOrderId = workOrderId;
    }
}
```

### Success Response
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

## ?? Execution Flow

### Sequence
```
1. Null check request
2. Validate request (structural)
3. Get work order from repository
4. Create PartItem (domain validates business rules)
5. Add part to work order (domain validates editability)
6. Persist changes
7. Return response
```

### Detailed Steps

#### 1. **Null Check**
```csharp
if (request is null)
    throw new ArgumentNullException(nameof(request));
```

**Protection:** Defensive programming against null references

---

#### 2. **Structural Validation**
```csharp
Validate(request);
```

**Application Layer Validates:**
- ? `WorkOrderId` is not empty
- ? `Code` is not empty or whitespace
- ? `Description` is not empty or whitespace

**NOT Validated Here (Domain Responsibility):**
- ? `UnitPrice > 0` (validated by PartItem constructor)
- ? `Quantity > 0` (validated by PartItem constructor)

**Throws:** `ValidationException` if validation fails

---

#### 3. **Get Work Order**
```csharp
var workOrder = await _workOrderRepository.GetByIdAsync(request.WorkOrderId, cancellationToken);
if (workOrder == null)
    throw new NotFoundException("WorkOrder", request.WorkOrderId);
```

**Throws:** `NotFoundException` if work order doesn't exist

---

#### 4. **Create Part Item (Value Object)**
```csharp
var partItem = new PartItem(
    code: request.Code,
    description: request.Description,
    unitPrice: request.UnitPrice,
    quantity: request.Quantity
);
```

**Domain Validation (PartItem constructor):**
- Code is not empty
- Description is not empty
- UnitPrice > 0
- Quantity > 0

**Throws:** `ValidationException` wrapping `ArgumentException` if domain validation fails

---

#### 5. **Add Part to Work Order (Domain Method)**
```csharp
workOrder.AddPart(partItem);
```

**Business Rules Enforced by Domain:**
```csharp
// Inside WorkOrder.AddPart()
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

**Valid Statuses:**
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

#### 6. **Persist Changes**
```csharp
await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
```

**Repository Updates:**
- Work order entity with new part
- Updated `UpdatedAt` timestamp
- Recalculated `TotalPrice`

---

#### 7. **Return Response**
```csharp
return new AddPartToWorkOrderResponse(workOrder.Id);
```

---

## ?? Business Rules Enforced

### 1. **Work Order Must Exist**
```csharp
if (workOrder == null)
    throw new NotFoundException("WorkOrder", request.WorkOrderId);
```

**HTTP Status:** 404 Not Found

---

### 2. **Work Order Must Be Editable**

| Status | Can Add Part? |
|--------|---------------|
| Received | ? Yes |
| InDiagnosis | ? Yes |
| WaitingForApproval | ? Yes |
| InExecution | ? Yes |
| Finished | ? No |
| Delivered | ? No |
| Cancelled | ? No |

---

### 3. **Part Item Must Be Valid**

**Application Layer Validates:**
- Code: Required (structural validation)
- Description: Required (structural validation)

**Domain Validates:**
- Code: Not empty (business rule)
- Description: Not empty (business rule)
- UnitPrice: Must be > 0 (business rule)
- Quantity: Must be > 0 (business rule)

---

## ?? Exception Handling

### ValidationException (400 Bad Request)

**Scenario 1: Invalid request structure**
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "Code": ["Part code is required."],
    "Description": ["Part description is required."]
  }
}
```

**Scenario 2: Domain validation failure**
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "PartItem": ["Unit price must be greater than zero."]
  }
}
```

**Scenario 3: Business rule violation**
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
_logger.LogInformation("Adding part to work order: {WorkOrderId}", request.WorkOrderId);
_logger.LogInformation("Part added successfully to work order: {WorkOrderId}", request.WorkOrderId);
```

#### Warning
```csharp
_logger.LogWarning("Work order not found: {WorkOrderId}", request.WorkOrderId);
_logger.LogWarning("Invalid part item: {Message}", ex.Message);
_logger.LogWarning("Cannot add part to work order: {Message}", ex.Message);
```

### Example Log Output
```
[2024-03-15 16:45:30] INFO  Adding part to work order: 3fa85f64-5717-4562-b3fc-2c963f66afa6
[2024-03-15 16:45:30] INFO  Part added successfully to work order: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## ?? Testing

### Unit Test Examples

```csharp
public class AddPartToWorkOrderUseCaseTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_AddsPartSuccessfully()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var request = new AddPartToWorkOrderRequest
        {
            WorkOrderId = workOrderId,
            Code = "FILTER-OIL-001",
            Description = "Engine oil filter",
            UnitPrice = 45.90m,
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

        var mockLogger = new Mock<ILogger<AddPartToWorkOrderUseCase>>();
        var useCase = new AddPartToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act
        var response = await useCase.HandleAsync(request);

        // Assert
        Assert.Equal(workOrderId, response.WorkOrderId);
        Assert.Equal(1, workOrder.Parts.Count);
        mockRepository.Verify(x => x.UpdateAsync(workOrder, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WorkOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new AddPartToWorkOrderRequest
        {
            WorkOrderId = Guid.NewGuid(),
            Code = "FILTER-OIL-001",
            Description = "Engine oil filter",
            UnitPrice = 45.90m,
            Quantity = 1
        };

        var mockRepository = new Mock<IWorkOrderRepository>();
        mockRepository
            .Setup(x => x.GetByIdAsync(request.WorkOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkOrder?)null);

        var mockLogger = new Mock<ILogger<AddPartToWorkOrderUseCase>>();
        var useCase = new AddPartToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_WorkOrderFinished_ThrowsValidationException()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var request = new AddPartToWorkOrderRequest
        {
            WorkOrderId = workOrderId,
            Code = "FILTER-OIL-001",
            Description = "Engine oil filter",
            UnitPrice = 45.90m,
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

        var mockLogger = new Mock<ILogger<AddPartToWorkOrderUseCase>>();
        var useCase = new AddPartToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.HandleAsync(request));
        Assert.Contains("WorkOrder", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("", "Oil filter", 45.90, 1, "Code")]
    [InlineData("FILTER-001", "", 45.90, 1, "Description")]
    public async Task HandleAsync_InvalidRequest_ThrowsValidationException(
        string code, string description, decimal unitPrice, int quantity, string expectedErrorKey)
    {
        // Arrange
        var request = new AddPartToWorkOrderRequest
        {
            WorkOrderId = Guid.NewGuid(),
            Code = code,
            Description = description,
            UnitPrice = unitPrice,
            Quantity = quantity
        };

        var mockRepository = new Mock<IWorkOrderRepository>();
        var mockLogger = new Mock<ILogger<AddPartToWorkOrderUseCase>>();
        var useCase = new AddPartToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.HandleAsync(request));
        Assert.Contains(expectedErrorKey, exception.Errors.Keys);
    }

    [Fact]
    public async Task HandleAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IWorkOrderRepository>();
        var mockLogger = new Mock<ILogger<AddPartToWorkOrderUseCase>>();
        var useCase = new AddPartToWorkOrderUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.HandleAsync(null!));
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
    private readonly AddPartToWorkOrderUseCase _addPartUseCase;

    public WorkOrdersController(AddPartToWorkOrderUseCase addPartUseCase)
    {
        _addPartUseCase = addPartUseCase;
    }

    [HttpPost("{workOrderId}/parts")]
    [ProducesResponseType(typeof(AddPartToWorkOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddPartToWorkOrderResponse>> AddPart(
        Guid workOrderId,
        [FromBody] AddPartToWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        request.WorkOrderId = workOrderId;

        try
        {
            var response = await _addPartUseCase.HandleAsync(request, cancellationToken);
            
            return Ok(new 
            { 
                workOrderId = response.WorkOrderId,
                success = true,
                message = "Part added successfully to work order."
            });
        }
        catch (NotFoundException)
        {
            return NotFound(new 
            { 
                success = false, 
                message = "Work order not found." 
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new 
            { 
                success = false, 
                message = "Validation failed.",
                errors = ex.Errors 
            });
        }
    }
}
```

### Example API Call
```bash
POST /api/work-orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/parts
Content-Type: application/json

{
  "code": "FILTER-OIL-001",
  "description": "Engine oil filter",
  "unitPrice": 45.90,
  "quantity": 1
}
```

**Success Response (200 OK):**
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "success": true,
  "message": "Part added successfully to work order."
}
```

---

## ?? Design Principles Applied

### ? **DDD: Domain Owns Business Rules**
- Application validates structure (Code, Description not empty)
- Domain validates business rules (UnitPrice > 0, Quantity > 0)
- Domain enforces editability rules

### ? **DRY: Don't Repeat Yourself**
- No duplicate validation between layers
- Trust domain to enforce its rules

### ? **KISS: Keep It Simple**
- Focused logging (Information + Warning only)
- Simple response (just WorkOrderId)
- Clean validation flow

### ? **Separation of Concerns**
- Application: orchestration + structural validation
- Domain: business rules + invariants
- API: HTTP semantics + presentation

---

## ?? Comparison with AddServiceToWorkOrder

| Aspect | AddServiceToWorkOrder | AddPartToWorkOrder |
|--------|----------------------|-------------------|
| **Request Fields** | Description, UnitPrice, Quantity | Code, Description, UnitPrice, Quantity |
| **Value Object** | `ServiceItem` | `PartItem` |
| **Domain Method** | `workOrder.AddService()` | `workOrder.AddPart()` |
| **Validation** | Description required | Code + Description required |
| **Business Rules** | Same (editability, pricing, quantity) | Same (editability, pricing, quantity) |
| **Logging** | Focused (Info + Warning) | Focused (Info + Warning) |
| **Response** | Simple (WorkOrderId only) | Simple (WorkOrderId only) |

---

## ?? Key Takeaways

1. **Consistent Pattern**: Follows same structure as AddServiceToWorkOrder
2. **Domain Protection**: Business rules enforced by domain layer
3. **Focused Validation**: Application handles structure, domain handles rules
4. **Simple Logging**: Essential events only (MVP-friendly)
5. **Clean Response**: No HTTP concerns in application layer
6. **Testability**: Easy to unit test with mocked dependencies

---

**This use case follows the same refined, production-ready pattern!** ??
