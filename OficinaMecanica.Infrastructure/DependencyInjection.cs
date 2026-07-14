using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Infrastructure.Persistence;
using OficinaMecanica.Infrastructure.Persistence.Repositories;
using OficinaMecanica.Infrastructure.Services;
using OficinaMecanica.Infrastructure.Settings;
using System.Text;

namespace OficinaMecanica.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ?? Banco de dados ??????????????????????????????
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

            // ?? Reposit�rios ????????????????????????????????
            services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IVeiculoRepository, VeiculoRepository>();
            services.AddScoped<IServicoRepository, ServicoRepository>();
            services.AddScoped<IPecaRepository, PecaRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // ?? Servi�os de dom�nio ?????????????????????????
            services.AddScoped<IGeradorNumeroOrdemServico, GeradorNumeroOrdemServico>();
            services.AddScoped<ISenhaService, BcryptSenhaService>();
            services.AddScoped<ITokenService, JwtTokenService>();

            // ?? E-mail ??????????????????????????????????????
            services.Configure<EmailSettings>(configuration.GetSection("Email"));
            services.AddScoped<IEmailService, SmtpEmailService>();

            // ?? JWT ?????????????????????????????????????????
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
                ?? throw new InvalidOperationException("Configura��o JWT ausente no appsettings.");

            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}

