using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
    {
        public void Configure(EntityTypeBuilder<OrdemServico> builder)
        {
            builder.ToTable("ordens_servico");

            builder.HasKey(os => os.Id)
                .HasName("pk_ordens_servico");

            builder.Property(os => os.Id)
                .ValueGeneratedNever();

            builder.Property(os => os.Numero)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(os => os.ClienteId)
                .IsRequired();

            builder.Property(os => os.VeiculoId)
                .IsRequired();

            builder.Property(os => os.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(os => os.Observacoes)
                .HasMaxLength(2000);

            builder.Property(os => os.IniciadaEm);
            builder.Property(os => os.FinalizadaEm);
            builder.Property(os => os.EntregueEm);

            builder.Property(os => os.CreatedAt)
                .IsRequired();

            builder.Property(os => os.UpdatedAt);

            builder.HasIndex(os => os.Numero)
                .IsUnique()
                .HasDatabaseName("ix_ordens_servico_numero");

            builder.HasIndex(os => os.ClienteId)
                .HasDatabaseName("ix_ordens_servico_cliente_id");

            builder.HasIndex(os => os.VeiculoId)
                .HasDatabaseName("ix_ordens_servico_veiculo_id");

            builder.HasIndex(os => os.Status)
                .HasDatabaseName("ix_ordens_servico_status");

            builder.HasIndex(os => os.CreatedAt)
                .HasDatabaseName("ix_ordens_servico_criada_em");

            // ItemServico como Owned Type (Value Object)
            var servicosNavigation = builder.Metadata.FindNavigation(nameof(OrdemServico.Servicos));
            servicosNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
            servicosNavigation?.SetField("_servicos");

            builder.OwnsMany(os => os.Servicos, servicos =>
            {
                servicos.ToTable("ordem_servico_servicos");

                servicos.WithOwner()
                    .HasForeignKey("ordem_servico_id")
                    .HasConstraintName("fk_ordem_servico_servicos_ordens_servico");

                servicos.Property<int>("id")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                servicos.HasKey("id")
                    .HasName("pk_ordem_servico_servicos");

                servicos.Property(s => s.ServicoId)
                    .IsRequired();

                servicos.Property(s => s.Descricao)
                    .IsRequired()
                    .HasMaxLength(500);

                servicos.Property(s => s.PrecoUnitario)
                    .IsRequired()
                    .HasPrecision(18, 2);

                servicos.Property(s => s.Quantidade)
                    .IsRequired();

                servicos.Ignore(s => s.TotalPrice);

                servicos.HasIndex("ordem_servico_id")
                    .HasDatabaseName("ix_ordem_servico_servicos_ordem_servico_id");
            });

            // ItemPeca como Owned Type (Value Object)
            var pecasNavigation = builder.Metadata.FindNavigation(nameof(OrdemServico.Pecas));
            pecasNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
            pecasNavigation?.SetField("_pecas");

            builder.OwnsMany(os => os.Pecas, pecas =>
            {
                pecas.ToTable("ordem_servico_pecas");

                pecas.WithOwner()
                    .HasForeignKey("ordem_servico_id")
                    .HasConstraintName("fk_ordem_servico_pecas_ordens_servico");

                pecas.Property<int>("id")
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                pecas.HasKey("id")
                    .HasName("pk_ordem_servico_pecas");

                pecas.Property(p => p.PecaId)
                    .IsRequired();

                pecas.Property(p => p.Codigo)
                    .IsRequired()
                    .HasMaxLength(50);

                pecas.Property(p => p.Descricao)
                    .IsRequired()
                    .HasMaxLength(500);

                pecas.Property(p => p.PrecoUnitario)
                    .IsRequired()
                    .HasPrecision(18, 2);

                pecas.Property(p => p.Quantidade)
                    .IsRequired();

                pecas.Ignore(p => p.TotalPrice);

                pecas.HasIndex(p => p.Codigo)
                    .HasDatabaseName("ix_ordem_servico_pecas_codigo");

                pecas.HasIndex("ordem_servico_id")
                    .HasDatabaseName("ix_ordem_servico_pecas_ordem_servico_id");
            });

            builder.Ignore(os => os.TotalServicos);
            builder.Ignore(os => os.TotalPecas);
            builder.Ignore(os => os.TotalOrcamento);
        }
    }
}
