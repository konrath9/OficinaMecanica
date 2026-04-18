using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Infrastructure.Persistence;
using OficinaMecanica.Infrastructure.Persistence.Repositories;
using OficinaMecanica.Infrastructure.Services;

namespace OficinaMecanica.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<OficinaMecanicaDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("OficinaMecanica.Infrastructure");
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    }));

            services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IVeiculoRepository, VeiculoRepository>();
            services.AddScoped<IServicoRepository, ServicoRepository>();
            services.AddScoped<IPecaRepository, PecaRepository>();

            services.AddScoped<IGeradorNumeroOrdemServico, GeradorNumeroOrdemServico>();

            return services;
        }
    }
}

