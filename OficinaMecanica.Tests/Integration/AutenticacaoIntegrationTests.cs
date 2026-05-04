using System.Net;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class AutenticacaoIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public AutenticacaoIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            factory.SeedAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Login_CredenciaisValidas_DeveRetornar200ComToken()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/autenticacao/login", new
            {
                email = "admin@oficina.com",
                senha = "Admin@123"
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<LoginResponseBody>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body.Token));
            Assert.Equal("Administrador", body.Perfil);
        }

        [Fact]
        public async Task Login_SenhaErrada_DeveRetornar401()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/autenticacao/login", new
            {
                email = "admin@oficina.com",
                senha = "senha_errada"
            });

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_EmailNaoExistente_DeveRetornar401()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/autenticacao/login", new
            {
                email = "naoexiste@email.com",
                senha = "qualquer"
            });

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Registrar_DadosValidos_DeveRetornar201()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/autenticacao/registrar", new
            {
                nome = "Mecânico Teste",
                email = "mecanico@oficina.com",
                senha = "Mecanico@123",
                perfil = 2 // Mecanico
            });

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Registrar_EmailDuplicado_DeveRetornar400()
        {
            // Act — primeiro registro
            await _client.PostAsJsonAsync("/api/autenticacao/registrar", new
            {
                nome = "Duplicado",
                email = "duplicado@oficina.com",
                senha = "Senha@123",
                perfil = 3
            });

            // Act — segundo registro com mesmo e-mail
            var response = await _client.PostAsJsonAsync("/api/autenticacao/registrar", new
            {
                nome = "Duplicado 2",
                email = "duplicado@oficina.com",
                senha = "Senha@123",
                perfil = 3
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private sealed record LoginResponseBody(string Token, string NomeUsuario, string Perfil, DateTime ExpiraEm);
    }
}
