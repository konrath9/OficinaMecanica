using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers");

            builder.HasKey(c => c.Id)
                .HasName("pk_customers");

            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Document)
                .IsRequired()
                .HasMaxLength(14);

            builder.Property(c => c.Email)
                .HasMaxLength(200);

            builder.Property(c => c.Phone)
                .HasMaxLength(20);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt);

            builder.HasIndex(c => c.Document)
                .IsUnique()
                .HasDatabaseName("ix_customers_document");
        }
    }
}
