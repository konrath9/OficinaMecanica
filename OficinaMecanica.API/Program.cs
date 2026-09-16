using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OficinaMecanica.API.Middleware;
using OficinaMecanica.Application;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Infrastructure;
using OficinaMecanica.Infrastructure.Persistence;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ?? Logs estruturados (JSON) ?????????????????????????????????????
// writeToProviders: o Serilog escreve no console E nos providers do MEL, para que o
// exportador OTLP de logs (configurado abaixo) tambem receba as mesmas mensagens.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()),
    writeToProviders: true);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OficinaMecanicaDbContext>();

// ?? Observabilidade (New Relic via OpenTelemetry/OTLP) ??????????????
// So habilita se houver license key configurada - em ambientes locais/CI sem a chave,
// a aplicacao roda normalmente sem tentar exportar nada.
var newRelicLicenseKey = builder.Configuration["NewRelic:LicenseKey"];
if (!string.IsNullOrWhiteSpace(newRelicLicenseKey))
{
    var otlpEndpoint = builder.Configuration["NewRelic:OtlpEndpoint"] ?? "https://otlp.nr-data.net:4317";

    void ConfigurarOtlp(OpenTelemetry.Exporter.OtlpExporterOptions otlp)
    {
        otlp.Endpoint = new Uri(otlpEndpoint);
        otlp.Headers = $"api-key={newRelicLicenseKey}";
    }

    // Logs: sem isto, o New Relic recebe traces e metricas mas nenhum log.
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("oficina-mecanica-api"));
        logging.IncludeScopes = true;
        logging.IncludeFormattedMessage = true;
        logging.AddOtlpExporter(ConfigurarOtlp);
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("oficina-mecanica-api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(ConfigurarOtlp))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(OrdemServicoMetrics.MeterName)
            .AddOtlpExporter(ConfigurarOtlp));
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// ?? Swagger com suporte a Bearer Token ??????????????????????????
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina Mec�nica API",
        Version = "v1",
        Description = "API administrativa da Oficina Mec�nica. Endpoints administrativos exigem autentica��o JWT."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT: Bearer {seu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Aplica migrations automaticamente ao iniciar (ignorado em testes com InMemory)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OficinaMecanicaDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// Swagger habilitado em todos os ambientes
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necess�rio para WebApplicationFactory nos testes de integra��o
public partial class Program { }
