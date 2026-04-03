# WorkOrder Domain Model - Design Decisions

## Overview
This document explains the domain modeling decisions for the `WorkOrder` aggregate following Domain-Driven Design (DDD) principles, aligned with the challenge requirements.

---

## 1. Entity Naming: `WorkOrder` (not `ServiceOrder`)

**Decision:** Used `WorkOrder` instead of `ServiceOrder`

**Rationale:**
- **Industry Standard:** "Work Order" is the most common term in automotive/mechanical workshop domains
- **Business Clarity:** Better represents the concept of "order of work to be performed"
- **International Standard:** More aligned with ISO standards and international automotive systems

---

## 2. Status Lifecycle (Aligned with Challenge Requirements)

### Status Enum
```csharp
public enum WorkOrderStatus
{
    Received = 1,          // Initial state when WO is created
    InDiagnosis = 2,       // Technician is diagnosing the vehicle
    WaitingForApproval = 3, // Budget sent to customer, awaiting approval
    InExecution = 4,       // Work is being performed
    Finished = 5,          // Work completed, vehicle ready
    Delivered = 6,         // Vehicle delivered to customer (final state)
    Cancelled = 7          // WO cancelled (final state)
}
```

### Key Changes from Initial Version
? **Removed (not in challenge requirements):**
- `WaitingForParts` - Not mentioned in PDF
- `Approved` - Redundant intermediate state
- `Invoiced` - Out of MVP scope

? **Kept (per challenge requirements):**
- All 7 status mentioned in the challenge document
- Clear state machine transitions
- Business rule validations

---

## 3. State Transitions & Business Rules

### Valid Transitions
```
Received ? InDiagnosis ? WaitingForApproval ? InExecution ? Finished ? Delivered
                                    ?
                              (Cancelled)
```

### Key Business Methods

#### `Approve()` - Direct Transition to Execution
```csharp
public void Approve()
{
    // Goes directly from WaitingForApproval to InExecution
    // No intermediate "Approved" status needed
}
```

**Rationale:**
- Simplifies state machine
- Customer approval immediately triggers work execution
- Aligns with real-world workflow

#### State Protection
- **Cannot edit:** Finished, Delivered, or Cancelled orders
- **Cannot cancel:** Finished or Delivered orders
- **Must have items:** To request approval or finish

---

## 4. Vehicle as Required Field

**Decision:** `VehicleId` is **required** (not nullable)

```csharp
public Guid VehicleId { get; private set; }  // Required

public WorkOrder(string number, Guid customerId, Guid vehicleId, ...)
{
    if (vehicleId == Guid.Empty)
        throw new ArgumentException("Vehicle is required.");
}
```

**Rationale:**
- Challenge explicitly states: "Associar veículo (placa, marca, modelo, ano)"
- Every work order in a mechanical workshop must be associated with a vehicle
- Business invariant: No vehicle = No work order

---

## 5. Value Objects without `Id` (DDD Purity)

### Before (Entity-like)
```csharp
public class ServiceItem
{
    public Guid Id { get; private set; }  // ? Makes it behave like Entity
    public string Description { get; private set; }
    // ...
}
```

### After (Pure Value Object)
```csharp
public class ServiceItem
{
    public string Description { get; private set; }  // ? Pure VO
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    // ...
}
```

**Rationale:**
- **DDD Definition:** Value Objects are identified by their attributes, not by identity
- **Immutability Emphasis:** No need for tracking identity
- **Business Perspective:** A "brake pad replacement at $50" is the same regardless of when it's added

### Removal Strategy
- **ServiceItem:** Removed by `Description` match
- **PartItem:** Removed by `Code` match

```csharp
public void RemoveService(string description) { ... }
public void RemovePart(string code) { ... }
```

---

## 6. Aggregate Root: `WorkOrder`

### Boundaries
**Entities Inside Aggregate:**
- `WorkOrder` (Root)

**Value Objects Inside Aggregate:**
- `ServiceItem` (list)
- `PartItem` (list)

**External References (by ID only):**
- `CustomerId` ? Customer aggregate (Cadastro Context)
- `VehicleId` ? Vehicle aggregate (Cadastro Context)

### Invariants Enforced
1. ? Must have at least one service OR part to request approval
2. ? Cannot modify finished/delivered/cancelled orders
3. ? State transitions follow business rules
4. ? Automatic timestamps on state changes
5. ? Total price calculated from items

---

## 7. Timestamp Properties

```csharp
public DateTime? StartedAt { get; private set; }     // When diagnosis started
public DateTime? FinishedAt { get; private set; }    // When work completed
public DateTime? DeliveredAt { get; private set; }  // When vehicle delivered
```

**Usage:**
- `StartedAt`: Set on `StartDiagnosis()` or first execution
- `FinishedAt`: Set on `Finish()`
- `DeliveredAt`: Set on `Deliver()`

**Business Value:**
- Track SLA compliance
- Calculate average execution time
- Identify bottlenecks

---

## 8. Removed Features (Out of Scope)

### `Invoice()` Method
? **Removed** - Invoicing is financial concern, not part of MVP work order management

### `WaitingForParts` Status
? **Removed** - Not in challenge requirements, adds unnecessary complexity for MVP

---

## 9. Architecture Alignment

### Clean Architecture Layers
```
OficinaMecanica.Domain
??? Common
?   ??? Entity.cs              (Base class)
??? Entities
?   ??? WorkOrder.cs           (Aggregate Root)
??? ValueObjects
?   ??? ServiceItem.cs
?   ??? PartItem.cs
??? Enums
    ??? WorkOrderStatus.cs
```

### DDD Patterns Applied
? Aggregate Pattern
? Value Object Pattern
? Entity Base Class
? Rich Domain Model (behavior not anemic)
? Ubiquitous Language (business terms)

---

## 10. Next Steps for Full Implementation

### Application Layer
- Use Cases (CQRS commands/queries)
- DTOs/ViewModels
- Validation layer

### Infrastructure Layer
- Repository implementations
- Entity Framework mappings
- Value Object conversions

### API Layer
- Controllers
- Request/Response models
- Swagger documentation

---

## Summary of Key Decisions

| Decision | Rationale |
|----------|-----------|
| Name: `WorkOrder` | Industry standard, clearer domain term |
| Status: 7 states only | Strict adherence to challenge requirements |
| `Approve()` ? `InExecution` | Simplified state machine, no intermediate status |
| `VehicleId` required | Business invariant from challenge |
| Value Objects without Id | DDD purity, true value semantics |
| Removed `Invoice()` | Out of MVP scope |
| Removed `WaitingForParts` | Not in requirements |

---

**This model is production-ready, testable, and fully aligned with the challenge requirements.** ??
