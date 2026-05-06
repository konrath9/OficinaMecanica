using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class PecaConfiguration : IEntityTypeConfiguration<Peca>
    {
        public void Configure(EntityTypeBuilder<Peca> builder)
        {
            builder.ToTable("pecas");

            builder.HasKey(p => p.Id)
                .HasName("pk_pecas");

            builder.Property(p => p.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.Codigo)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(p => p.Nome)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.PrecoUnitario)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(p => p.QuantidadeEstoque)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt);

            builder.HasIndex(p => p.Codigo)
                .IsUnique()
                .HasDatabaseName("ix_pecas_codigo");

            builder.HasData(SeedData.Pecas);
        }
    }
}
