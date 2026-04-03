using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
    {
        public void Configure(EntityTypeBuilder<WorkOrder> builder)
        {
            // Table configuration
            builder.ToTable("work_orders");

            // Primary key with explicit naming
            builder.HasKey(wo => wo.Id)
                .HasName("pk_work_orders");

            // Properties
            builder.Property(wo => wo.Id)
                .ValueGeneratedNever(); // Guid is generated in domain

            builder.Property(wo => wo.Number)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(wo => wo.CustomerId)
                .IsRequired();

            builder.Property(wo => wo.VehicleId)
                .IsRequired();

            builder.Property(wo => wo.Status)
                .IsRequired()
                .HasConversion<string>() // Store enum as string for readability
                .HasMaxLength(50);

            builder.Property(wo => wo.Notes)
                .HasMaxLength(2000);

            builder.Property(wo => wo.StartedAt);

            builder.Property(wo => wo.FinishedAt);

            builder.Property(wo => wo.DeliveredAt);

            builder.Property(wo => wo.CreatedAt)
                .IsRequired();

            builder.Property(wo => wo.UpdatedAt);

            // Indexes with explicit naming (standard PostgreSQL convention)
            builder.HasIndex(wo => wo.Number)
                .IsUnique()
                .HasDatabaseName("ix_work_orders_number");

            builder.HasIndex(wo => wo.CustomerId)
                .HasDatabaseName("ix_work_orders_customer_id");

            builder.HasIndex(wo => wo.VehicleId)
                .HasDatabaseName("ix_work_orders_vehicle_id");

            builder.HasIndex(wo => wo.Status)
                .HasDatabaseName("ix_work_orders_status");

            builder.HasIndex(wo => wo.CreatedAt)
                .HasDatabaseName("ix_work_orders_created_at");

            // Configure ServiceItem as Owned Type (Value Object)
            // Map to private field to maintain encapsulation
            var servicesNavigation = builder.Metadata.FindNavigation(nameof(WorkOrder.Services));
            servicesNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
            servicesNavigation?.SetField("_services");

            builder.OwnsMany(wo => wo.Services, services =>
            {
                services.ToTable("work_order_services");

                services.WithOwner()
                    .HasForeignKey("work_order_id")
                    .HasConstraintName("fk_work_order_services_work_orders");

                // Value Objects don't have identity in domain, but need surrogate key for database
                services.Property<int>("id")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                services.HasKey("id")
                    .HasName("pk_work_order_services");

                services.Property(s => s.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                services.Property(s => s.UnitPrice)
                    .IsRequired()
                    .HasPrecision(18, 2);

                services.Property(s => s.Quantity)
                    .IsRequired();

                services.Ignore(s => s.TotalPrice); // Calculated property, not persisted

                // Index on foreign key
                services.HasIndex("work_order_id")
                    .HasDatabaseName("ix_work_order_services_work_order_id");
            });

            // Configure PartItem as Owned Type (Value Object)
            // Map to private field to maintain encapsulation
            var partsNavigation = builder.Metadata.FindNavigation(nameof(WorkOrder.Parts));
            partsNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
            partsNavigation?.SetField("_parts");

            builder.OwnsMany(wo => wo.Parts, parts =>
            {
                parts.ToTable("work_order_parts");

                parts.WithOwner()
                    .HasForeignKey("work_order_id")
                    .HasConstraintName("fk_work_order_parts_work_orders");

                // Value Objects don't have identity in domain, but need surrogate key for database
                parts.Property<int>("id")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                parts.HasKey("id")
                    .HasName("pk_work_order_parts");

                parts.Property(p => p.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                parts.Property(p => p.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                parts.Property(p => p.UnitPrice)
                    .IsRequired()
                    .HasPrecision(18, 2);

                parts.Property(p => p.Quantity)
                    .IsRequired();

                parts.Ignore(p => p.TotalPrice); // Calculated property, not persisted

                // Indexes
                parts.HasIndex(p => p.Code)
                    .HasDatabaseName("ix_work_order_parts_code");

                parts.HasIndex("work_order_id")
                    .HasDatabaseName("ix_work_order_parts_work_order_id");
            });

            // Ignore calculated properties (computed from owned collections)
            builder.Ignore(wo => wo.TotalServicesPrice);
            builder.Ignore(wo => wo.TotalPartsPrice);
            builder.Ignore(wo => wo.TotalPrice);
        }
    }
}
