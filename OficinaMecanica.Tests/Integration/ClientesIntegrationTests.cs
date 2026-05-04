using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class ClientesIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public ClientesIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            factory.SeedAsync().GetAwaiter().GetResult();
        }

        private async Task AutenticarAsync()
        {
            var token = await _factory.ObterTokenAsync(_client);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetClientes_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/clientes");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetClientes_ComToken_DeveRetornar200()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/clientes");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostCliente_ComDadosValidos_DeveRetornar201()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Cliente Integração",
                documento = "529.982.247-25",
                email = "cliente@teste.com",
                telefone = "11999999999"
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostCliente_DocumentoInvalido_DeveRetornar400()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Cliente Inválido",
                documento = "111.111.111-11"
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostCliente_EmailInvalido_DeveRetornar400()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Cliente Email Inválido",
                documento = "529.982.247-25",
                email = "email-sem-arroba"
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostCliente_TelefoneInvalido_DeveRetornar400()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Cliente Tel Inválido",
                documento = "529.982.247-25",
                telefone = "123"
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FluxoCRUD_Cliente_DeveRealizarTodasAsOperacoes()
        {
            await AutenticarAsync();

            // POST
            var criar = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "João CRUD",
                documento = "111.444.777-35",
                email = "joao@crud.com",
                telefone = "11988887777"
            });
            Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
            var criado = await criar.Content.ReadFromJsonAsync<IdResponse>();

            // GET por ID
            var get = await _client.GetAsync($"/api/clientes/{criado!.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // GET por documento
            var getDoc = await _client.GetAsync("/api/clientes/por-documento/11144477735");
            Assert.Equal(HttpStatusCode.OK, getDoc.StatusCode);

            // PUT
            var put = await _client.PutAsJsonAsync($"/api/clientes/{criado.Id}", new
            {
                nome = "João CRUD Atualizado",
                email = "joao.novo@crud.com"
            });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            // DELETE
            var delete = await _client.DeleteAsync($"/api/clientes/{criado.Id}");
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Fact]
        public async Task GetCliente_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync($"/api/clientes/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutCliente_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.PutAsJsonAsync($"/api/clientes/{Guid.NewGuid()}", new
            {
                nome = "Ninguém"
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCliente_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.DeleteAsync($"/api/clientes/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetClientePorDocumento_DocumentoInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/clientes/por-documento/98765432100");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostCliente_DocumentoDuplicado_DeveRetornar400()
        {
            await AutenticarAsync();

            await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Primeiro",
                documento = "123.456.789-09"
            });

            var response = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Segundo",
                documento = "123.456.789-09"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
