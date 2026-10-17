using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Infrastructure.Persistence;

namespace OficinaMecanica.Infrastructure.Mensageria
{
    public static class MensageriaExtensions
    {
        /// <summary>
        /// MassTransit + RabbitMQ com a Saga persistida no Postgres (mesmo DbContext da OS) e outbox transacional:
        /// a mudança na OS, o estado da Saga e as mensagens publicadas são gravados no mesmo SaveChanges.
        /// Sem RabbitMq:Host configurado (testes, dev sem broker), usa transporte e repositório em memória.
        /// </summary>
        public static IServiceCollection AddMensageria(this IServiceCollection services, IConfiguration configuration)
        {
            var host = configuration["RabbitMq:Host"];

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                var saga = x.AddSagaStateMachine<OrdemServicoStateMachine, OrdemServicoSagaState>();

                if (string.IsNullOrWhiteSpace(host))
                {
                    saga.InMemoryRepository();
                    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                    return;
                }

                saga.EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    r.ExistingDbContext<OficinaMecanicaDbContext>();
                    r.UsePostgres();
                });

                x.AddEntityFrameworkOutbox<OficinaMecanicaDbContext>(o =>
                {
                    o.UsePostgres();
                    o.UseBusOutbox();
                });

                x.AddConfigureEndpointsCallback((context, _, cfg) =>
                    cfg.UseEntityFrameworkOutbox<OficinaMecanicaDbContext>(context));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(host, "/", h =>
                    {
                        h.Username(configuration["RabbitMq:Username"] ?? "guest");
                        h.Password(configuration["RabbitMq:Password"] ?? "guest");
                    });
                    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
