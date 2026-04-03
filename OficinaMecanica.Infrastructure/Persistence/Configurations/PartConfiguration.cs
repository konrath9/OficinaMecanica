using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class PartConfiguration : IEntityTypeConfiguration<Part>
    {
        public void Configure(EntityTypeBuilder<Part> builder)
        {
            builder.ToTable("parts");

            builder.HasKey(p => p.Id)
                .HasName("pk_parts");

            builder.Property(p => p.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(p => p.StockQuantity)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt);

            builder.HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("ix_parts_code");
        }
    }
}
