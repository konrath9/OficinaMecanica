# EF Core Migrations Guide - OficinaMecanica

## ?? Configuração Completa

### ? PASSO 1 - Pacotes Instalados

**Infrastructure Project:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**API Project:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

---

### ? PASSO 2 - Connection String

**appsettings.json (Production):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=oficina_mecanica;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**appsettings.Development.json (Development):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=oficina_mecanica_dev;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Migrations": "Information"
    }
  }
}
```

---

### ? PASSO 3 - DbContext Registration

**Program.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration
builder.Services.AddDbContext<OficinaMecanicaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("OficinaMecanica.Infrastructure");
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));
```

---

## ?? Comandos de Migração

### Criar Nova Migration
```bash
dotnet ef migrations add InitialCreate \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API \
  --context OficinaMecanicaDbContext
```

### Aplicar Migration no Banco de Dados
```bash
dotnet ef database update \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API \
  --context OficinaMecanicaDbContext
```

### Remover Última Migration
```bash
dotnet ef migrations remove \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API \
  --context OficinaMecanicaDbContext
```

### Gerar Script SQL
```bash
dotnet ef migrations script \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API \
  --context OficinaMecanicaDbContext \
  --output migration.sql
```

### Listar Migrations
```bash
dotnet ef migrations list \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API \
  --context OficinaMecanicaDbContext
```

### Reverter para Migration Específica
```bash
dotnet ef database update <MigrationName> \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API \
  --context OficinaMecanicaDbContext
```

---

## ?? Schema Gerado

### work_orders (Main Table)
```sql
CREATE TABLE work_orders (
    id UUID PRIMARY KEY,
    number VARCHAR(50) NOT NULL UNIQUE,
    customer_id UUID NOT NULL,
    vehicle_id UUID NOT NULL,
    status INTEGER NOT NULL,
    notes VARCHAR(2000),
    started_at TIMESTAMP WITH TIME ZONE,
    finished_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- Indexes
CREATE UNIQUE INDEX i_x_work_orders_number ON work_orders(number);
CREATE INDEX i_x_work_orders_customer_id ON work_orders(customer_id);
CREATE INDEX i_x_work_orders_vehicle_id ON work_orders(vehicle_id);
CREATE INDEX i_x_work_orders_status ON work_orders(status);
CREATE INDEX i_x_work_orders_created_at ON work_orders(created_at);
```

### work_order_services (Owned Type)
```sql
CREATE TABLE work_order_services (
    id SERIAL PRIMARY KEY,
    work_order_id UUID NOT NULL,
    description VARCHAR(500) NOT NULL,
    unit_price NUMERIC(18,2) NOT NULL,
    quantity INTEGER NOT NULL,
    FOREIGN KEY (work_order_id) REFERENCES work_orders(id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX i_x_work_order_services_work_order_id ON work_order_services(work_order_id);
```

### work_order_parts (Owned Type)
```sql
CREATE TABLE work_order_parts (
    id SERIAL PRIMARY KEY,
    work_order_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL,
    description VARCHAR(500) NOT NULL,
    unit_price NUMERIC(18,2) NOT NULL,
    quantity INTEGER NOT NULL,
    FOREIGN KEY (work_order_id) REFERENCES work_orders(id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX i_x_work_order_parts_work_order_id ON work_order_parts(work_order_id);
CREATE INDEX i_x_work_order_parts_code ON work_order_parts(code);
```

---

## ?? PostgreSQL Setup

### Usando Docker
```bash
docker run --name postgres-oficina \
  -e POSTGRES_DB=oficina_mecanica \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### Verificar se está rodando
```bash
docker ps
```

### Conectar ao PostgreSQL
```bash
docker exec -it postgres-oficina psql -U postgres -d oficina_mecanica
```

### Comandos Úteis PostgreSQL
```sql
-- Listar tabelas
\dt

-- Descrever tabela
\d work_orders

-- Verificar dados
SELECT * FROM work_orders;

-- Verificar foreign keys
SELECT 
    tc.constraint_name, 
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY';
```

---

## ?? Troubleshooting

### Erro: "No executable found matching command dotnet-ef"
```bash
dotnet tool install --global dotnet-ef
```

### Erro: "Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design"
```bash
dotnet add OficinaMecanica.API package Microsoft.EntityFrameworkCore.Design --version 8.0.0
```

### Erro: "Unable to create an object of type 'DbContext'"
- Verificar se `Program.cs` tem `AddDbContext`
- Verificar connection string em `appsettings.json`
- Verificar se projeto API tem referência ao Infrastructure

### Erro: "The ConnectionString property has not been initialized"
- Verificar nome da connection string: `"DefaultConnection"`
- Verificar se `appsettings.json` está no projeto API
- Verificar se projeto está usando configuração correta (Development vs Production)

---

## ?? Migration Files

A migration criada contém:

```
OficinaMecanica.Infrastructure/Migrations/
??? 20260403180548_InitialCreate.cs                    # Migration Up/Down methods
??? 20260403180548_InitialCreate.Designer.cs           # Migration metadata
??? OficinaMecanicaDbContextModelSnapshot.cs           # Current model snapshot
```

**InitialCreate.cs** contains:
- `Up()`: Creates tables, indexes, foreign keys
- `Down()`: Drops everything (rollback)

**ModelSnapshot.cs** contains:
- Current state of the database model
- Used for comparing changes in next migrations

---

## ?? Next Steps

### 1. Apply Migration to Database
```bash
dotnet ef database update \
  --project OficinaMecanica.Infrastructure \
  --startup-project OficinaMecanica.API
```

### 2. Verify Tables Created
```bash
# Connect to PostgreSQL
docker exec -it postgres-oficina psql -U postgres -d oficina_mecanica

# List tables
\dt

# Should show:
#  work_orders
#  work_order_services
#  work_order_parts
#  __EFMigrationsHistory
```

### 3. Create Repository Implementation
```csharp
public class WorkOrderRepository : IWorkOrderRepository
{
    private readonly OficinaMecanicaDbContext _context;

    public WorkOrderRepository(OficinaMecanicaDbContext context)
    {
        _context = context;
    }

    public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkOrders
            .FirstOrDefaultAsync(wo => wo.Id == id, cancellationToken);
    }

    public async Task<WorkOrder> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync(cancellationToken);
        return workOrder;
    }

    public async Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        _context.WorkOrders.Update(workOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### 4. Register Repository in DI
```csharp
// Program.cs
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
```

---

## ? Success Checklist

- [x] Pacotes EF Core instalados
- [x] Connection string configurada
- [x] DbContext registrado no DI
- [x] Migration criada
- [ ] Migration aplicada no banco (run `dotnet ef database update`)
- [ ] Tabelas verificadas no PostgreSQL
- [ ] Repository implementado
- [ ] Repository registrado no DI
- [ ] Controller criado
- [ ] Testes realizados

---

**Migration created successfully! Ready to update database! ??**
