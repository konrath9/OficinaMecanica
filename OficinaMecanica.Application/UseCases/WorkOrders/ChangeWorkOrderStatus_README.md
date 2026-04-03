# ChangeWorkOrderStatus Use Case

## ?? Overview
This use case handles work order status transitions by delegating to domain methods, ensuring all business rules are enforced by the domain layer.

---

## ?? Use Case: ChangeWorkOrderStatusUseCase

### Responsibility
Changes the status of a work order by executing the appropriate domain method based on the requested action.

### Dependencies
```csharp
public ChangeWorkOrderStatusUseCase(
    IWorkOrderRepository workOrderRepository,
    ILogger<ChangeWorkOrderStatusUseCase> logger)
```

---

## ?? Input (Request DTO)

```csharp
public class ChangeWorkOrderStatusRequest
{
    public Guid WorkOrderId { get; set; }      // Required - Work order to change
    public string Action { get; set; }         // Required - Action to execute
}
```

### Valid Actions
```csharp
- "StartDiagnosis"       // Received ? InDiagnosis
- "RequestApproval"      // InDiagnosis ? WaitingForApproval
- "Approve"              // WaitingForApproval ? InExecution
- "Finish"               // InExecution ? Finished
- "Deliver"              // Finished ? Delivered
- "Cancel"               // Any (except Finished/Delivered) ? Cancelled
```

### Example Request
```json
{
  "workOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "action": "StartDiagnosis"
}
```

---

## ?? Output (Response DTO)

```csharp
public class ChangeWorkOrderStatusResponse
{
    public Guid WorkOrderId { get; set; }
    
    public ChangeWorkOrderStatusResponse(Guid workOrderId)
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
2. Validate request (WorkOrderId, Action)
3. Get work order from repository
4. Execute action (delegate to domain)
5. Persist changes
6. Return response
```

### Detailed Steps

#### 1. **Null Check**
```csharp
if (request is null)
    throw new ArgumentNullException(nameof(request));
```

---

#### 2. **Validation**
```csharp
Validate(request);
```

**Application Layer Validates:**
- ? `WorkOrderId` is not empty
- ? `Action` is not empty
- ? `Action` is in the list of valid actions

**Valid Actions List:**
```csharp
private static readonly HashSet<string> ValidActions = new()
{
    "StartDiagnosis",
    "RequestApproval",
    "Approve",
    "Finish",
    "Deliver",
    "Cancel"
};
```

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

#### 4. **Execute Action (Delegate to Domain)**
```csharp
try
{
    ExecuteAction(workOrder, request.Action);
}
catch (InvalidOperationException ex)
{
    throw new ValidationException("WorkOrder", ex.Message);
}
```

**Key Design Decision:**
- ? **NO business logic in use case**
- ? **Switch only for method dispatch**
- ? **Domain enforces all rules**
- ? **Clean separation of concerns**

**ExecuteAction Method:**
```csharp
private static void ExecuteAction(WorkOrder workOrder, string action)
{
    // Delegate to domain methods - NO business logic here
    switch (action)
    {
        case "StartDiagnosis":
            workOrder.StartDiagnosis();
            break;

        case "RequestApproval":
            workOrder.RequestApproval();
            break;

        case "Approve":
            workOrder.Approve();
            break;

        case "Finish":
            workOrder.Finish();
            break;

        case "Deliver":
            workOrder.Deliver();
            break;

        case "Cancel":
            workOrder.Cancel();
            break;

        default:
            throw new ArgumentException($"Unknown action: {action}");
    }
}
```

**Why This Design:**
- ? **Simple dispatcher**: Only routes to correct method
- ? **No duplication**: Business rules only in domain
- ? **Easy to test**: Mock domain methods
- ? **DDD-compliant**: Domain owns behavior

---

#### 5. **Persist Changes**
```csharp
await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
```

**Repository Updates:**
- Updated status
- Updated timestamps (StartedAt, FinishedAt, DeliveredAt)
- Updated `UpdatedAt`

---

#### 6. **Return Response**
```csharp
return new ChangeWorkOrderStatusResponse(workOrder.Id);
```

---

## ?? Business Rules (Enforced by Domain)

### Status Transition Rules

#### StartDiagnosis
```csharp
// Domain: WorkOrder.StartDiagnosis()
if (Status != WorkOrderStatus.Received)
    throw new InvalidOperationException("Only received orders can start diagnosis.");

// Transition: Received ? InDiagnosis
// Sets: StartedAt = DateTime.UtcNow
```

---

#### RequestApproval
```csharp
// Domain: WorkOrder.RequestApproval()
if (Status != WorkOrderStatus.InDiagnosis)
    throw new InvalidOperationException("Only orders in diagnosis can request approval.");

if (!_services.Any() && !_parts.Any())
    throw new InvalidOperationException("The order must have at least one service or part to request approval.");

// Transition: InDiagnosis ? WaitingForApproval
```

---

#### Approve
```csharp
// Domain: WorkOrder.Approve()
if (Status != WorkOrderStatus.WaitingForApproval)
    throw new InvalidOperationException("Only orders waiting for approval can be approved.");

// Transition: WaitingForApproval ? InExecution
// Sets: StartedAt = DateTime.UtcNow (if not already set)
```

---

#### Finish
```csharp
// Domain: WorkOrder.Finish()
if (Status != WorkOrderStatus.InExecution)
    throw new InvalidOperationException("Only orders in execution can be finished.");

if (!_services.Any() && !_parts.Any())
    throw new InvalidOperationException("The order must have at least one service or part to be finished.");

// Transition: InExecution ? Finished
// Sets: FinishedAt = DateTime.UtcNow
```

---

#### Deliver
```csharp
// Domain: WorkOrder.Deliver()
if (Status != WorkOrderStatus.Finished)
    throw new InvalidOperationException("Only finished orders can be delivered.");

// Transition: Finished ? Delivered
// Sets: DeliveredAt = DateTime.UtcNow
```

---

#### Cancel
```csharp
// Domain: WorkOrder.Cancel()
if (Status == WorkOrderStatus.Finished || Status == WorkOrderStatus.Delivered)
    throw new InvalidOperationException("Finished or delivered orders cannot be cancelled.");

// Transition: Any (except Finished/Delivered) ? Cancelled
// Optionally accepts reason parameter
```

---

## ?? State Machine

```
???????????????
?  Received   ? ??[StartDiagnosis]???
???????????????                     ?
                            ????????????????
                            ? InDiagnosis  ?
                            ????????????????
                                   ?
                         [RequestApproval]
                                   ?
                      ???????????????????????
                      ? WaitingForApproval  ?
                      ???????????????????????
                             ?
                        [Approve]
                             ?
                      ???????????????
                      ? InExecution ?
                      ???????????????
                             ?
                         [Finish]
                             ?
                      ???????????????
                      ?  Finished   ?
                      ???????????????
                             ?
                        [Deliver]
                             ?
                      ???????????????
                      ?  Delivered  ? (final)
                      ???????????????

                      [Cancel] can be called from any state
                      (except Finished/Delivered)
                             ?
                      ???????????????
                      ?  Cancelled  ? (final)
                      ???????????????
```

---

## ?? Exception Handling

### ValidationException (400 Bad Request)

**Scenario 1: Invalid action**
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "Action": ["Invalid action. Valid actions are: StartDiagnosis, RequestApproval, Approve, Finish, Deliver, Cancel"]
  }
}
```

**Scenario 2: Invalid state transition**
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "WorkOrder": ["Only received orders can start diagnosis."]
  }
}
```

**Scenario 3: Business rule violation**
```json
{
  "type": "ValidationException",
  "title": "One or more validation failures have occurred.",
  "errors": {
    "WorkOrder": ["The order must have at least one service or part to request approval."]
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

### Information
```csharp
_logger.LogInformation("Changing work order status: {WorkOrderId}, Action: {Action}", 
    request.WorkOrderId, request.Action);
    
_logger.LogInformation("Work order status changed successfully: {WorkOrderId}, Action: {Action}", 
    request.WorkOrderId, request.Action);
```

### Warning
```csharp
_logger.LogWarning("Work order not found: {WorkOrderId}", request.WorkOrderId);

_logger.LogWarning("Cannot execute action {Action} on work order {WorkOrderId}: {Message}", 
    request.Action, request.WorkOrderId, ex.Message);
    
_logger.LogWarning("Invalid action {Action} for work order {WorkOrderId}: {Message}", 
    request.Action, request.WorkOrderId, ex.Message);
```

### Example Log Output
```
[2024-03-15 17:30:45] INFO  Changing work order status: 3fa85f64-5717-4562-b3fc-2c963f66afa6, Action: StartDiagnosis
[2024-03-15 17:30:45] INFO  Work order status changed successfully: 3fa85f64-5717-4562-b3fc-2c963f66afa6, Action: StartDiagnosis
```

---

## ?? Testing

### Unit Test Examples

```csharp
public class ChangeWorkOrderStatusUseCaseTests
{
    [Fact]
    public async Task HandleAsync_StartDiagnosis_ChangesStatusSuccessfully()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var request = new ChangeWorkOrderStatusRequest
        {
            WorkOrderId = workOrderId,
            Action = "StartDiagnosis"
        };

        var workOrder = new WorkOrder("WO-001", Guid.NewGuid(), Guid.NewGuid());
        
        var mockRepository = new Mock<IWorkOrderRepository>();
        mockRepository
            .Setup(x => x.GetByIdAsync(workOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workOrder);

        var mockLogger = new Mock<ILogger<ChangeWorkOrderStatusUseCase>>();
        var useCase = new ChangeWorkOrderStatusUseCase(mockRepository.Object, mockLogger.Object);

        // Act
        var response = await useCase.HandleAsync(request);

        // Assert
        Assert.Equal(workOrderId, response.WorkOrderId);
        Assert.Equal(WorkOrderStatus.InDiagnosis, workOrder.Status);
        Assert.NotNull(workOrder.StartedAt);
        mockRepository.Verify(x => x.UpdateAsync(workOrder, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidAction_ThrowsValidationException()
    {
        // Arrange
        var request = new ChangeWorkOrderStatusRequest
        {
            WorkOrderId = Guid.NewGuid(),
            Action = "InvalidAction"
        };

        var mockRepository = new Mock<IWorkOrderRepository>();
        var mockLogger = new Mock<ILogger<ChangeWorkOrderStatusUseCase>>();
        var useCase = new ChangeWorkOrderStatusUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.HandleAsync(request));
        Assert.Contains("Action", exception.Errors.Keys);
    }

    [Fact]
    public async Task HandleAsync_InvalidTransition_ThrowsValidationException()
    {
        // Arrange
        var workOrderId = Guid.NewGuid();
        var request = new ChangeWorkOrderStatusRequest
        {
            WorkOrderId = workOrderId,
            Action = "Finish"  // Can't finish a Received order
        };

        var workOrder = new WorkOrder("WO-001", Guid.NewGuid(), Guid.NewGuid());
        // Status is Received by default

        var mockRepository = new Mock<IWorkOrderRepository>();
        mockRepository
            .Setup(x => x.GetByIdAsync(workOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workOrder);

        var mockLogger = new Mock<ILogger<ChangeWorkOrderStatusUseCase>>();
        var useCase = new ChangeWorkOrderStatusUseCase(mockRepository.Object, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.HandleAsync(request));
        Assert.Contains("WorkOrder", exception.Errors.Keys);
        Assert.Contains("Only orders in execution can be finished", exception.Errors["WorkOrder"][0]);
    }

    [Theory]
    [InlineData("StartDiagnosis")]
    [InlineData("RequestApproval")]
    [InlineData("Approve")]
    [InlineData("Finish")]
    [InlineData("Deliver")]
    [InlineData("Cancel")]
    public async Task HandleAsync_AllValidActions_AreRecognized(string action)
    {
        // Arrange
        var request = new ChangeWorkOrderStatusRequest
        {
            WorkOrderId = Guid.NewGuid(),
            Action = action
        };

        var mockRepository = new Mock<IWorkOrderRepository>();
        var mockLogger = new Mock<ILogger<ChangeWorkOrderStatusUseCase>>();
        var useCase = new ChangeWorkOrderStatusUseCase(mockRepository.Object, mockLogger.Object);

        // Act
        var exception = await Record.ExceptionAsync(() => useCase.HandleAsync(request));

        // Assert - should fail for other reasons (not found), not invalid action
        Assert.IsNotType<ValidationException>(exception);
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
    private readonly ChangeWorkOrderStatusUseCase _changeStatusUseCase;

    public WorkOrdersController(ChangeWorkOrderStatusUseCase changeStatusUseCase)
    {
        _changeStatusUseCase = changeStatusUseCase;
    }

    [HttpPost("{workOrderId}/status")]
    [ProducesResponseType(typeof(ChangeWorkOrderStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChangeWorkOrderStatusResponse>> ChangeStatus(
        Guid workOrderId,
        [FromBody] ChangeWorkOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        request.WorkOrderId = workOrderId;

        try
        {
            var response = await _changeStatusUseCase.HandleAsync(request, cancellationToken);
            
            return Ok(new 
            { 
                workOrderId = response.WorkOrderId,
                success = true,
                message = $"Status changed successfully. Action: {request.Action}"
            });
        }
        catch (NotFoundException)
        {
            return NotFound(new { success = false, message = "Work order not found." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { success = false, errors = ex.Errors });
        }
    }
}
```

### Example API Calls

**Start Diagnosis:**
```bash
POST /api/work-orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/status
Content-Type: application/json

{
  "action": "StartDiagnosis"
}
```

**Request Approval:**
```bash
POST /api/work-orders/3fa85f64-5717-4562-b3fc-2c963f66afa6/status
Content-Type: application/json

{
  "action": "RequestApproval"
}
```

---

## ?? Design Principles Applied

### ? **DDD: Domain Owns Behavior**
```csharp
// ? DON'T: Business logic in use case
if (workOrder.Status == WorkOrderStatus.Received)
{
    workOrder.Status = WorkOrderStatus.InDiagnosis;
    workOrder.StartedAt = DateTime.UtcNow;
}

// ? DO: Delegate to domain
workOrder.StartDiagnosis();
```

### ? **Single Responsibility**
- Application: orchestrates, dispatches
- Domain: enforces rules, validates transitions

### ? **Open/Closed Principle**
- Adding new actions: add to ValidActions list and switch case
- Business rules: change only in domain

### ? **Command Pattern**
- Action represents a command
- Use case dispatches to appropriate handler (domain method)

---

## ?? Key Takeaways

1. **Switch for Dispatch Only**: Not for business logic
2. **Domain Methods Are Clear**: Each method name matches the action
3. **All Rules in Domain**: Status transitions, validations, invariants
4. **Simple Use Case**: Just gets, dispatches, saves
5. **Testable**: Easy to test both layers separately

---

**This use case demonstrates clean DDD separation and proper orchestration!** ??
