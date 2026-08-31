using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OficinaMecanica.Application;
using OficinaMecanica.Application.Common.Metrics;
using OficinaMecanica.Infrastructure;
using OficinaMecanica.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

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

// Swagger habilitado em todos os ambientes
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necess�rio para WebApplicationFactory nos testes de integra��o
public partial class Program { }
