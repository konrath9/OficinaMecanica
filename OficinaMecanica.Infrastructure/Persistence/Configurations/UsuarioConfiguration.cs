using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuarios");

            builder.HasKey(u => u.Id)
                .HasName("pk_usuarios");

            builder.Property(u => u.Id)
                .ValueGeneratedNever();

            builder.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.SenhaHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.Perfil)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(u => u.Ativo)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt);

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_usuarios_email");
        }
    }
}
