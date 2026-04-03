# CreateWorkOrder Use Case - Application Layer

## Overview
This use case handles the creation of new Work Orders in the system following Clean Architecture and DDD principles.

---

## Architecture

### Layer Structure
```
Application Layer (OficinaMecanica.Application)
??? UseCases/
?   ??? WorkOrders/
?       ??? CreateWorkOrderUseCase.cs        (Main use case)
??? DTOs/
?   ??? WorkOrders/
?       ??? CreateWorkOrderRequest.cs        (Input)
?       ??? CreateWorkOrderResponse.cs       (Output)
??? Interfaces/
?   ??? Repositories/
?   ?   ??? IWorkOrderRepository.cs          (Data access abstraction)
?   ??? Services/
?       ??? IWorkOrderNumberGenerator.cs     (Business service)
??? Common/
    ??? Exceptions/
        ??? ValidationException.cs
        ??? NotFoundException.cs
```

---

## Use Case: CreateWorkOrderUseCase

### Responsibility
Creates a new Work Order with initial status `Received` and persists it to the database.

### Dependencies
```csharp
public CreateWorkOrderUseCase(
    IWorkOrderRepository workOrderRepository,      // Data persistence
    IWorkOrderNumberGenerator numberGenerator)      // Business logic for numbering
```

### Input (Request DTO)
```csharp
public class CreateWorkOrderRequest
{
    public Guid CustomerId { get; set; }    // Required
    public Guid VehicleId { get; set; }     // Required
    public string? Notes { get; set; }      // Optional
}
```

### Output (Response DTO)
```csharp
public class CreateWorkOrderResponse
{
    public Guid Id { get; set; }            // Generated work order ID
    public string Number { get; set; }      // Generated work order number
    public string Status { get; set; }      // Always "Received"
    public DateTime CreatedAt { get; set; } // Creation timestamp
}
```

---

## Execution Flow

### 1. **Validation**
```csharp
ValidateRequest(request);
```
- ? Validates `CustomerId` is not empty
- ? Validates `VehicleId` is not empty
- ? Throws `ValidationException` if validation fails

### 2. **Number Generation**
```csharp
var workOrderNumber = await _numberGenerator.GenerateAsync(cancellationToken);
```
- Delegates to domain service
- Example formats: "WO-2024-0001", "OS-001234", etc.

### 3. **Entity Creation**
```csharp
var workOrder = new WorkOrder(
    number: workOrderNumber,
    customerId: request.CustomerId,
    vehicleId: request.VehicleId,
    notes: request.Notes
);
```
- Uses domain entity constructor
- Domain enforces business rules
- Initial status: `WorkOrderStatus.Received`

### 4. **Persistence**
```csharp
var createdWorkOrder = await _workOrderRepository.AddAsync(workOrder, cancellationToken);
```
- Repository pattern abstracts data access
- Returns persisted entity with ID

### 5. **Response Mapping**
```csharp
return new CreateWorkOrderResponse(
    id: createdWorkOrder.Id,
    number: createdWorkOrder.Number,
    status: createdWorkOrder.Status.ToString(),
    createdAt: createdWorkOrder.CreatedAt
);
```

---

## Interfaces

### IWorkOrderRepository
```csharp
public interface IWorkOrderRepository
{
    Task<WorkOrder> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken);
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken);
    Task<IEnumerable<WorkOrder>> GetAllAsync(CancellationToken cancellationToken);
    Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
```

**Implementation:** Infrastructure Layer (e.g., Entity Framework)

### IWorkOrderNumberGenerator
```csharp
public interface IWorkOrderNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken);
}
```

**Implementation Example:**
```csharp
// Infrastructure/Services/SequentialWorkOrderNumberGenerator.cs
public async Task<string> GenerateAsync(CancellationToken cancellationToken)
{
    var lastNumber = await _repository.GetLastNumberAsync(cancellationToken);
    var nextSequence = ParseSequence(lastNumber) + 1;
    return $"WO-{DateTime.UtcNow:yyyy}-{nextSequence:D4}";
}
```

---

## Exception Handling

### ValidationException
```csharp
throw new ValidationException(new Dictionary<string, string[]>
{
    { "CustomerId", new[] { "Customer ID is required." } },
    { "VehicleId", new[] { "Vehicle ID is required." } }
});
```

**HTTP Response:** 400 Bad Request
```json
{
    "type": "ValidationException",
    "title": "One or more validation failures have occurred.",
    "errors": {
        "CustomerId": ["Customer ID is required."],
        "VehicleId": ["Vehicle ID is required."]
    }
}
```

### Domain Validation Exception
```csharp
catch (ArgumentException ex)
{
    throw new ValidationException("WorkOrder", ex.Message);
}
```

**Example:** If domain constructor throws for invalid VehicleId

---

## Usage Example (API Controller)

```csharp
[ApiController]
[Route("api/work-orders")]
public class WorkOrdersController : ControllerBase
{
    private readonly CreateWorkOrderUseCase _createUseCase;

    public WorkOrdersController(CreateWorkOrderUseCase createUseCase)
    {
        _createUseCase = createUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<CreateWorkOrderResponse>> Create(
        [FromBody] CreateWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _createUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }
}
```

---

## Dependency Injection Setup

```csharp
// Program.cs or Startup.cs
services.AddScoped<CreateWorkOrderUseCase>();
services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
services.AddScoped<IWorkOrderNumberGenerator, SequentialWorkOrderNumberGenerator>();
```

---

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task ExecuteAsync_ValidRequest_ReturnsCreatedWorkOrder()
{
    // Arrange
    var request = new CreateWorkOrderRequest
    {
        CustomerId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        Notes = "Customer reported engine noise"
    };

    var mockRepository = new Mock<IWorkOrderRepository>();
    var mockNumberGenerator = new Mock<IWorkOrderNumberGenerator>();
    
    mockNumberGenerator
        .Setup(x => x.GenerateAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync("WO-2024-0001");

    mockRepository
        .Setup(x => x.AddAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((WorkOrder wo, CancellationToken ct) => wo);

    var useCase = new CreateWorkOrderUseCase(mockRepository.Object, mockNumberGenerator.Object);

    // Act
    var response = await useCase.ExecuteAsync(request);

    // Assert
    Assert.NotEqual(Guid.Empty, response.Id);
    Assert.Equal("WO-2024-0001", response.Number);
    Assert.Equal("Received", response.Status);
    mockRepository.Verify(x => x.AddAsync(It.IsAny<WorkOrder>(), It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task ExecuteAsync_EmptyCustomerId_ThrowsValidationException()
{
    // Arrange
    var request = new CreateWorkOrderRequest
    {
        CustomerId = Guid.Empty,
        VehicleId = Guid.NewGuid()
    };

    var mockRepository = new Mock<IWorkOrderRepository>();
    var mockNumberGenerator = new Mock<IWorkOrderNumberGenerator>();
    var useCase = new CreateWorkOrderUseCase(mockRepository.Object, mockNumberGenerator.Object);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => useCase.ExecuteAsync(request)
    );

    Assert.Contains("CustomerId", exception.Errors.Keys);
}
```

---

## Design Principles Applied

### ? Single Responsibility Principle (SRP)
- Use case has one reason to change: work order creation logic
- Repository handles persistence
- Number generator handles numbering logic

### ? Dependency Inversion Principle (DIP)
- Depends on abstractions (`IWorkOrderRepository`, `IWorkOrderNumberGenerator`)
- No direct dependencies on infrastructure

### ? Interface Segregation Principle (ISP)
- Small, focused interfaces
- `IWorkOrderNumberGenerator` has single method

### ? Open/Closed Principle (OCP)
- Can extend numbering strategies without modifying use case
- Can swap repository implementations

### ? Clean Architecture
- Application layer doesn't know about database or HTTP
- DTOs separate from domain entities
- Clear boundaries between layers

---

## Next Steps

1. **Implement Infrastructure:**
   - `WorkOrderRepository` (Entity Framework)
   - `SequentialWorkOrderNumberGenerator`

2. **Add More Use Cases:**
   - `GetWorkOrderByIdUseCase`
   - `UpdateWorkOrderStatusUseCase`
   - `AddServiceToWorkOrderUseCase`

3. **Add Validation:**
   - FluentValidation for request DTOs
   - Check if Customer exists
   - Check if Vehicle exists

4. **Add Events:**
   - `WorkOrderCreatedEvent`
   - Notify mechanics
   - Update dashboards

---

**This use case is production-ready, testable, and follows SOLID principles!** ??
