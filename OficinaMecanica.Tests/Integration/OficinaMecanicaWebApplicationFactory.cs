using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Infrastructure.Persistence;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class OficinaMecanicaWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"OficinaMecanica_Testes_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove o DbContext real (PostgreSQL)
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OficinaMecanicaDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Adiciona InMemory com nome unico por instancia de factory
                services.AddDbContext<OficinaMecanicaDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });

            builder.UseEnvironment("Testing");
        }

        /// <summary>Popula o banco em memória com dados iniciais para os testes.</summary>
        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OficinaMecanicaDbContext>();
            var senhaService = scope.ServiceProvider.GetRequiredService<ISenhaService>();

            await context.Database.EnsureCreatedAsync();

            if (!context.Usuarios.Any())
            {
                context.Usuarios.Add(new Usuario(
                    nome: "Administrador",
                    email: "admin@oficina.com",
                    senhaHash: senhaService.Criptografar("Admin@123"),
                    perfil: PerfilUsuario.Administrador));

                await context.SaveChangesAsync();
            }
        }

        /// <summary>Realiza login e retorna o token JWT para uso nos testes autenticados.</summary>
        public async Task<string> ObterTokenAsync(HttpClient client,
            string email = "admin@oficina.com", string senha = "Admin@123")
        {
            var response = await client.PostAsJsonAsync("/api/autenticacao/login",
                new { email, senha });

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
            return body!.Token;
        }

        private sealed record TokenResponse(string Token);
    }
}
