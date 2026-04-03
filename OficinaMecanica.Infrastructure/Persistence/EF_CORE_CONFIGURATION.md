# Entity Framework Core Configuration - DDD with PostgreSQL

## ?? Overview
This document explains the EF Core configuration for the Work Order management system, following DDD principles and PostgreSQL conventions.

---

## ?? Database Structure

### Tables Created

```
work_orders (Main table - Aggregate Root)
??? work_order_services (Owned Type - Value Objects)
??? work_order_parts (Owned Type - Value Objects)
```

---

## ??? Architecture Decisions

### 1. **Owned Types for Value Objects** ? DDD-COMPLIANT

#### ServiceItem and PartItem as Owned Types
```csharp
builder.OwnsMany(wo => wo.Services, services =>
{
    services.ToTable("work_order_services");
    // Configuration...
});

builder.OwnsMany(wo => wo.Parts, parts =>
{
    parts.ToTable("work_order_parts");
    // Configuration...
});
```

**Why Owned Types:**

? **DDD Alignment**
```csharp
// ? DON'T: Value Objects as independent entities
public DbSet<ServiceItem> ServiceItems { get; set; }  // Wrong! Breaks aggregate

// ? DO: Value Objects owned by aggregate
builder.OwnsMany(wo => wo.Services);  // Correct! Maintains aggregate boundary
```

? **No Separate Repository Needed**
- ServiceItems can't be queried independently
- Always loaded with WorkOrder (aggregate root)
- Can't exist without WorkOrder

? **Maintains Encapsulation**
```csharp
// Domain has private list:
private readonly List<ServiceItem> _services;

// EF Core can still map it:
builder.OwnsMany(wo => wo.Services);
```

? **Cascade Delete Automatic**
- Delete WorkOrder ? ServiceItems automatically deleted
- No orphaned records

---

### 2. **Snake Case Convention for PostgreSQL** ? POSTGRESQL BEST PRACTICE

#### Automatic Conversion
```csharp
private static void ConfigurePostgreSqlConventions(ModelBuilder modelBuilder)
{
    // WorkOrders ? work_orders
    // CustomerId ? customer_id
    // CreatedAt ? created_at
}
```

**Why Snake Case:**

? **PostgreSQL Convention**
```sql
-- PostgreSQL standard naming
SELECT * FROM work_orders WHERE customer_id = '...';

-- Not: SELECT * FROM WorkOrders WHERE CustomerId = '...';
```

? **Case Insensitive**
```sql
-- All equivalent in PostgreSQL
SELECT * FROM work_orders;
SELECT * FROM WORK_ORDERS;
SELECT * FROM Work_Orders;
```

? **Easier SQL Queries**
```sql
-- Clean and readable
SELECT 
    wo.id,
    wo.number,
    wo.customer_id,
    wo.status
FROM work_orders wo
WHERE wo.status = 4;
```

---

### 3. **Shadow Properties for Foreign Keys** ? CLEAN DOMAIN

#### Owned Types Use Shadow Properties
```csharp
parts.WithOwner()
    .HasForeignKey("work_order_id");  // ? Shadow property (not in domain)

parts.Property<int>("Id")
    .HasColumnName("id")
    .ValueGeneratedOnAdd();  // ? Shadow property for surrogate key
```

**Why Shadow Properties:**

? **Domain Stays Clean**
```csharp
// Domain: PartItem (Value Object)
public class PartItem
{
    // ? No WorkOrderId property
    // ? No database-specific Id
    
    public string Code { get; private set; }
    public string Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    
    // ? Pure domain logic only
}
```

? **EF Core Manages Relationships**
```csharp
// Infrastructure: EF Core adds what it needs
parts.Property<int>("Id")  // Surrogate key for database
parts.WithOwner().HasForeignKey("work_order_id")  // FK to parent
```

---

### 4. **Enum Stored as Integer** ? PERFORMANCE

```csharp
builder.Property(wo => wo.Status)
    .HasConversion<int>();
```

**Why Integer:**

? **Smaller Storage**
```sql
-- Integer: 4 bytes
status INTEGER

-- vs String: 20+ bytes
status VARCHAR(50)
```

? **Better Performance**
```sql
-- Integer comparison is faster
WHERE status = 4

-- vs String comparison
WHERE status = 'InExecution'
```

? **Index Efficiency**
```sql
CREATE INDEX ix_work_orders_status ON work_orders(status);
-- Integer index is more efficient
```

**Database Values:**
```sql
-- WorkOrderStatus enum values
1 = Received
2 = InDiagnosis
3 = WaitingForApproval
4 = InExecution
5 = Finished
6 = Delivered
7 = Cancelled
```

---

### 5. **Calculated Properties Ignored** ? NO REDUNDANCY

```csharp
builder.Ignore(wo => wo.TotalServicesPrice);
builder.Ignore(wo => wo.TotalPartsPrice);
builder.Ignore(wo => wo.TotalPrice);

services.Ignore(s => s.TotalPrice);
parts.Ignore(p => p.TotalPrice);
```

**Why Ignore:**

? **Computed on the Fly**
```csharp
// Domain calculates when needed
public decimal TotalServicesPrice => _services.Sum(s => s.TotalPrice);
public decimal TotalPrice => TotalServicesPrice + TotalPartsPrice;
```

? **No Data Redundancy**
```sql
-- ? DON'T store calculated values
-- total_services_price DECIMAL(18,2)  -- Can become out of sync!

-- ? DO calculate in query when needed
SELECT 
    wo.id,
    (SELECT SUM(unit_price * quantity) FROM work_order_services WHERE work_order_id = wo.id) as total_services_price
FROM work_orders wo;
```

? **Single Source of Truth**
- Values always accurate
- No synchronization issues
- Simpler schema

---

### 6. **Indexes for Performance** ? QUERY OPTIMIZATION

```csharp
builder.HasIndex(wo => wo.Number).IsUnique();
builder.HasIndex(wo => wo.CustomerId);
builder.HasIndex(wo => wo.Status);
builder.HasIndex(wo => wo.CreatedAt);
```

**Index Strategy:**

? **Unique Index on Number**
```sql
CREATE UNIQUE INDEX ix_work_orders_number ON work_orders(number);
-- Ensures no duplicate work order numbers
-- Fast lookup by work order number
```

? **Foreign Key Indexes**
```sql
CREATE INDEX ix_work_orders_customer_id ON work_orders(customer_id);
CREATE INDEX ix_work_orders_vehicle_id ON work_orders(vehicle_id);
-- Fast queries: "Find all work orders for customer X"
```

? **Status Index**
```sql
CREATE INDEX ix_work_orders_status ON work_orders(status);
-- Fast queries: "Find all in-progress work orders"
```

? **Date Index**
```sql
CREATE INDEX ix_work_orders_created_at ON work_orders(created_at);
-- Fast queries: "Find work orders created this month"
```

---

## ?? Database Schema

### work_orders Table
```sql
CREATE TABLE work_orders (
    id UUID PRIMARY KEY,
    number VARCHAR(50) NOT NULL UNIQUE,
    customer_id UUID NOT NULL,
    vehicle_id UUID NOT NULL,
    status INTEGER NOT NULL,
    notes VARCHAR(2000),
    started_at TIMESTAMP,
    finished_at TIMESTAMP,
    delivered_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);
```

### work_order_services Table (Owned Type)
```sql
CREATE TABLE work_order_services (
    id SERIAL PRIMARY KEY,
    work_order_id UUID NOT NULL,
    description VARCHAR(500) NOT NULL,
    unit_price DECIMAL(18,2) NOT NULL,
    quantity INTEGER NOT NULL,
    FOREIGN KEY (work_order_id) REFERENCES work_orders(id) ON DELETE CASCADE
);
```

### work_order_parts Table (Owned Type)
```sql
CREATE TABLE work_order_parts (
    id SERIAL PRIMARY KEY,
    work_order_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL,
    description VARCHAR(500) NOT NULL,
    unit_price DECIMAL(18,2) NOT NULL,
    quantity INTEGER NOT NULL,
    FOREIGN KEY (work_order_id) REFERENCES work_orders(id) ON DELETE CASCADE
);
```

---

## ?? EF Core Usage Examples

### Query Work Order with Services and Parts
```csharp
var workOrder = await _context.WorkOrders
    .FirstOrDefaultAsync(wo => wo.Id == workOrderId);

// Services and Parts are automatically loaded (owned types)
var totalPrice = workOrder.TotalPrice;  // Calculated from loaded collections
```

### Add Work Order
```csharp
var workOrder = new WorkOrder("WO-001", customerId, vehicleId);
workOrder.AddService(new ServiceItem("Oil change", 89.90m, 1));
workOrder.AddPart(new PartItem("FILTER-001", "Oil filter", 25.00m, 1));

_context.WorkOrders.Add(workOrder);
await _context.SaveChangesAsync();

// EF Core automatically:
// 1. Inserts into work_orders
// 2. Inserts into work_order_services
// 3. Inserts into work_order_parts
```

### Update Work Order
```csharp
var workOrder = await _context.WorkOrders
    .FirstOrDefaultAsync(wo => wo.Id == workOrderId);

workOrder.StartDiagnosis();
workOrder.AddService(new ServiceItem("Brake inspection", 120.00m, 1));

await _context.SaveChangesAsync();

// EF Core automatically:
// 1. Updates work_orders (status, started_at)
// 2. Inserts into work_order_services
```

### Delete Work Order
```csharp
var workOrder = await _context.WorkOrders
    .FirstOrDefaultAsync(wo => wo.Id == workOrderId);

_context.WorkOrders.Remove(workOrder);
await _context.SaveChangesAsync();

// EF Core automatically:
// 1. Deletes from work_order_services (cascade)
// 2. Deletes from work_order_parts (cascade)
// 3. Deletes from work_orders
```

---

## ?? Design Principles Applied

### ? **DDD: Aggregate Boundary**
- WorkOrder is the aggregate root
- ServiceItem and PartItem are value objects owned by the aggregate
- No direct access to services/parts tables

### ? **Encapsulation**
- Private collections in domain (`_services`, `_parts`)
- EF Core can still map them
- No setters exposed

### ? **Performance**
- Appropriate indexes
- Decimal precision for money (18,2)
- Integer enum storage
- Snake case for PostgreSQL

### ? **Data Integrity**
- Required fields enforced
- Unique constraints
- Foreign keys with cascade delete
- Max length constraints

---

## ?? Connection String Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=oficina_mecanica;Username=postgres;Password=yourpassword"
  }
}
```

---

## ?? Migration Commands

### Create Migration
```bash
dotnet ef migrations add InitialCreate --project OficinaMecanica.Infrastructure --startup-project OficinaMecanica.API
```

### Update Database
```bash
dotnet ef database update --project OficinaMecanica.Infrastructure --startup-project OficinaMecanica.API
```

### Generate SQL Script
```bash
dotnet ef migrations script --project OficinaMecanica.Infrastructure --startup-project OficinaMecanica.API --output migration.sql
```

---

## ?? Key Takeaways

1. **Owned Types Preserve DDD**
   - Value objects stay as value objects
   - No separate DbSets needed
   - Aggregate boundary respected

2. **Shadow Properties Keep Domain Clean**
   - Database concerns stay in infrastructure
   - Domain models remain pure

3. **PostgreSQL Conventions**
   - Snake case naming
   - Appropriate data types
   - Efficient indexes

4. **Performance Optimized**
   - Integer enums
   - Calculated properties not stored
   - Strategic indexes

5. **EF Core Power**
   - Automatic cascade deletes
   - Fluent API for precise control
   - Convention-based mapping

---

**This configuration demonstrates professional-grade EF Core usage with DDD principles!** ??
