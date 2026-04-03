using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence
{
    public class OficinaMecanicaDbContext : DbContext
    {
        public OficinaMecanicaDbContext(DbContextOptions<OficinaMecanicaDbContext> options)
            : base(options)
        {
        }

        public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Part> Parts => Set<Part>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaMecanicaDbContext).Assembly);

            // PostgreSQL specific configurations
            ConfigurePostgreSqlConventions(modelBuilder);
        }

        private static void ConfigurePostgreSqlConventions(ModelBuilder modelBuilder)
        {
            // Use snake_case for table and column names only (PostgreSQL convention)
            // Keys and indexes use standard naming (pk_, ix_, fk_) without snake_case conversion
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Table names: WorkOrders -> work_orders
                entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

                // Column names: WorkOrderId -> work_order_id
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.GetColumnName().ToSnakeCase());
                }

                // Keys and indexes: keep standard naming without double underscores
                // They will be named properly in entity configurations
            }
        }
    }

    // Extension method for snake_case conversion
    internal static class StringExtensions
    {
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLowerInvariant(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(input[i]));
                }
                else
                {
                    result.Append(input[i]);
                }
            }

            return result.ToString();
        }
    }
}
