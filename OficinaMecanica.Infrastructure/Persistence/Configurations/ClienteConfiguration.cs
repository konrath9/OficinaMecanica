using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("clientes");

            builder.HasKey(c => c.Id)
                .HasName("pk_clientes");

            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Documento)
                .IsRequired()
                .HasMaxLength(14);

            builder.Property(c => c.Email)
                .HasMaxLength(200);

            builder.Property(c => c.Telefone)
                .HasMaxLength(20);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt);

            builder.HasIndex(c => c.Documento)
                .IsUnique()
                .HasDatabaseName("ix_clientes_documento");
        }
    }
}
