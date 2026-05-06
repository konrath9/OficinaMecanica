using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
    {
        public void Configure(EntityTypeBuilder<Servico> builder)
        {
            builder.ToTable("servicos");

            builder.HasKey(s => s.Id)
                .HasName("pk_servicos");

            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.Nome)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Descricao)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(s => s.Preco)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.UpdatedAt);

            builder.HasIndex(s => s.Nome)
                .HasDatabaseName("ix_servicos_nome");

            builder.HasData(SeedData.Servicos);
        }
    }
}
