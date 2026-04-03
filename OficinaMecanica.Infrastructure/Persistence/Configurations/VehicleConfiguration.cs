using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.ToTable("vehicles");

            builder.HasKey(v => v.Id)
                .HasName("pk_vehicles");

            builder.Property(v => v.Id)
                .ValueGeneratedNever();

            builder.Property(v => v.LicensePlate)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(v => v.Brand)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(v => v.Model)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(v => v.Year)
                .IsRequired();

            builder.Property(v => v.CustomerId)
                .IsRequired();

            builder.Property(v => v.CreatedAt)
                .IsRequired();

            builder.Property(v => v.UpdatedAt);

            builder.HasIndex(v => v.LicensePlate)
                .IsUnique()
                .HasDatabaseName("ix_vehicles_license_plate");

            builder.HasIndex(v => v.CustomerId)
                .HasDatabaseName("ix_vehicles_customer_id");

            builder.HasOne(v => v.Customer)
                .WithMany()
                .HasForeignKey(v => v.CustomerId)
                .HasConstraintName("fk_vehicles_customers")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
