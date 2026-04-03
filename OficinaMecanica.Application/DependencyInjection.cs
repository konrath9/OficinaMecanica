using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Application.UseCases.WorkOrders;

namespace OficinaMecanica.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<CreateWorkOrderUseCase>();
            services.AddScoped<AddServiceToWorkOrderUseCase>();
            services.AddScoped<AddPartToWorkOrderUseCase>();
            services.AddScoped<ChangeWorkOrderStatusUseCase>();

            return services;
        }
    }
}
