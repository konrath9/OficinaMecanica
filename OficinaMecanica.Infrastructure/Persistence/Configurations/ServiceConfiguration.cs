using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("services");

            builder.HasKey(s => s.Id)
                .HasName("pk_services");

            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(s => s.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.UpdatedAt);

            builder.HasIndex(s => s.Name)
                .HasDatabaseName("ix_services_name");
        }
    }
}
