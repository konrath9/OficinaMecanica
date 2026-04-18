using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
    {
        public void Configure(EntityTypeBuilder<Veiculo> builder)
        {
            builder.ToTable("veiculos");

            builder.HasKey(v => v.Id)
                .HasName("pk_veiculos");

            builder.Property(v => v.Id)
                .ValueGeneratedNever();

            builder.Property(v => v.Placa)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(v => v.Marca)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(v => v.Modelo)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(v => v.Ano)
                .IsRequired();

            builder.Property(v => v.ClienteId)
                .IsRequired();

            builder.Property(v => v.CreatedAt)
                .IsRequired();

            builder.Property(v => v.UpdatedAt);

            builder.HasIndex(v => v.Placa)
                .IsUnique()
                .HasDatabaseName("ix_veiculos_placa");

            builder.HasIndex(v => v.ClienteId)
                .HasDatabaseName("ix_veiculos_cliente_id");

            builder.HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .HasConstraintName("fk_veiculos_clientes")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
