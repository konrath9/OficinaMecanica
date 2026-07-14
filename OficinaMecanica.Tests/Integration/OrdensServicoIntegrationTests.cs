using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class OrdensServicoIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public OrdensServicoIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            factory.SeedAsync().GetAwaiter().GetResult();
        }

        // ??????????????????????????????????????????????
        // Prote��o JWT
        // ??????????????????????????????????????????????

        [Fact]
        public async Task GetOrdensServico_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/ordens-servico");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetClientes_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/clientes");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetVeiculos_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/veiculos");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetServicos_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/servicos");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPecas_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/pecas");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ??????????????????????????????????????????????
        // Acesso autenticado
        // ??????????????????????????????????????????????

        [Fact]
        public async Task GetOrdensServico_ComToken_DeveRetornar200()
        {
            var token = await _factory.ObterTokenAsync(_client);
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/ordens-servico");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetClientes_ComToken_DeveRetornar200()
        {
            var token = await _factory.ObterTokenAsync(_client);
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/clientes");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // ??????????????????????????????????????????????
        // Endpoint p�blico � sem autentica��o
        // ??????????????????????????????????????????????

        [Fact]
        public async Task AcompanhamentoPublico_SemToken_DeveRetornar404OuOK()
        {
            // Sem token � endpoint p�blico deve responder (404 = OS n�o encontrada, n�o 401)
            var response = await _client.GetAsync("/api/acompanhamento/OS-NAO-EXISTE");

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ??????????????????????????????????????????????
        // Fluxo completo: Cliente ? Ve�culo ? OS
        // ??????????????????????????????????????????????

        [Fact]
        public async Task FluxoCriacaoOS_ClienteVeiculoOS_DeveRetornar201()
        {
            var token = await _factory.ObterTokenAsync(_client);
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 1 � Criar cliente
            var clienteResponse = await _client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Cliente Integra��o",
                documento = "529.982.247-25",
                email = "integracao@teste.com",
                telefone = "11999999999"
            });
            Assert.Equal(HttpStatusCode.Created, clienteResponse.StatusCode);
            var cliente = await clienteResponse.Content.ReadFromJsonAsync<IdResponse>();

            // 2 � Criar ve�culo
            var veiculoResponse = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa = "TST1234",
                marca = "Toyota",
                modelo = "Corolla",
                ano = 2020,
                clienteId = cliente!.Id
            });
            Assert.Equal(HttpStatusCode.Created, veiculoResponse.StatusCode);
            var veiculo = await veiculoResponse.Content.ReadFromJsonAsync<IdResponse>();

            // 3 � Criar OS
            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId = cliente.Id,
                veiculoId = veiculo!.Id,
                observacoes = "Revis�o completa"
            });
            Assert.Equal(HttpStatusCode.Created, osResponse.StatusCode);
        }

        private sealed record IdResponse(Guid Id);
    }
}
