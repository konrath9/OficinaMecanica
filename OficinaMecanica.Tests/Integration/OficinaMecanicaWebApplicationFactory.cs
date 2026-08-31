using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Infrastructure.Persistence;
using OficinaMecanica.Infrastructure.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

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

        /// <summary>Popula o banco em mem�ria com dados iniciais para os testes.</summary>
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

        /// <summary>
        /// Constroi um token JWT de Cliente igual ao que a Function Serverless (Fase 3, repositorio
        /// separado) vai emitir apos validar o CPF: mesma assinatura HS256/Issuer/Audience da API,
        /// claim de role "Cliente" e sub = clienteId. Usado nos testes pra validar a checagem de posse
        /// do AcompanhamentoController sem precisar da Lambda de verdade.
        /// </summary>
        public string GerarTokenCliente(Guid clienteId, string nome = "Cliente Teste")
        {
            var settings = Services.GetRequiredService<IOptions<JwtSettings>>().Value;
            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, nome),
                new Claim(ClaimTypes.Role, "Cliente"),
                new Claim("perfil", "Cliente"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credenciais);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private sealed record TokenResponse(string Token);
    }
}
