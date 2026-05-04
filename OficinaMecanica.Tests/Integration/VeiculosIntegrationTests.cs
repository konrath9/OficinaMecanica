using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class VeiculosIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public VeiculosIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
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

        private async Task<Guid> CriarClienteAsync(string documento = "111.444.777-35", string nome = "Cliente Veiculo")
        {
            var response = await _client.PostAsJsonAsync("/api/clientes", new { nome, documento });
            var body = await response.Content.ReadFromJsonAsync<IdResponse>();
            return body!.Id;
        }

        [Fact]
        public async Task GetVeiculos_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/veiculos");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetVeiculos_ComToken_DeveRetornar200()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/veiculos");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostVeiculo_ComDadosValidos_DeveRetornar201()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("111.444.777-35", "Dono do Carro");

            var response = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "ABC-1234",
                marca = "Toyota",
                modelo = "Corolla",
                ano = 2020,
                clienteId
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostVeiculo_PlacaMercosul_DeveRetornar201()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("123.456.789-09", "Dono Mercosul");

            var response = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "ABC1D23",
                marca = "Honda",
                modelo = "Civic",
                ano = 2022,
                clienteId
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostVeiculo_ClienteInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "XYZ-9999",
                marca = "Ford",
                modelo = "Ka",
                ano = 2019,
                clienteId = Guid.NewGuid()
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostVeiculo_PlacaInvalida_DeveRetornar400()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("987.654.321-00", "Cliente Placa Inv");

            var response = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "INVALIDA",
                marca = "Ford",
                modelo = "Ka",
                ano = 2019,
                clienteId
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FluxoCRUD_Veiculo_DeveRealizarTodasAsOperacoes()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("456.789.123-64", "Dono CRUD");

            // POST
            var criar = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "TST-0001",
                marca = "Chevrolet",
                modelo = "Onix",
                ano = 2021,
                clienteId
            });
            Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
            var criado = await criar.Content.ReadFromJsonAsync<IdResponse>();

            // GET por ID
            var get = await _client.GetAsync($"/api/veiculos/{criado!.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // GET por cliente
            var getCliente = await _client.GetAsync($"/api/veiculos/por-cliente/{clienteId}");
            Assert.Equal(HttpStatusCode.OK, getCliente.StatusCode);

            // PUT
            var put = await _client.PutAsJsonAsync($"/api/veiculos/{criado.Id}", new
            {
                marca = "Chevrolet",
                modelo = "Onix Plus",
                ano = 2022
            });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            // DELETE
            var delete = await _client.DeleteAsync($"/api/veiculos/{criado.Id}");
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Fact]
        public async Task GetVeiculo_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync($"/api/veiculos/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutVeiculo_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.PutAsJsonAsync($"/api/veiculos/{Guid.NewGuid()}", new
            {
                marca = "X",
                modelo = "Y",
                ano = 2020
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteVeiculo_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.DeleteAsync($"/api/veiculos/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostVeiculo_PlacaDuplicada_DeveRetornar400()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("789.123.456-64", "Dono Dup");

            await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "DUP-0001",
                marca = "Ford",
                modelo = "Fusion",
                ano = 2018,
                clienteId
            });

            var response = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "DUP-0001",
                marca = "Ford",
                modelo = "Fusion",
                ano = 2018,
                clienteId
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
