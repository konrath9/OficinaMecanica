using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaMecanica.Infrastructure.Mensageria;

namespace OficinaMecanica.Infrastructure.Persistence.Configurations
{
    public class OrdemServicoSagaStateConfiguration : IEntityTypeConfiguration<OrdemServicoSagaState>
    {
        public void Configure(EntityTypeBuilder<OrdemServicoSagaState> builder)
        {
            builder.ToTable("saga_ordem_servico");
            builder.HasKey(s => s.CorrelationId).HasName("pk_saga_ordem_servico");
            builder.Property(s => s.CorrelationId).ValueGeneratedNever();
            builder.Property(s => s.CurrentState).IsRequired().HasMaxLength(64);
            builder.Property(s => s.Valor).HasPrecision(18, 2);
            builder.Property(s => s.UltimoMotivo).HasMaxLength(500);
            builder.HasIndex(s => s.CurrentState).HasDatabaseName("ix_saga_ordem_servico_current_state");
        }
    }
}
