using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class PecasIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public PecasIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
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
        public async Task GetPecas_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/pecas");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPecas_ComToken_DeveRetornar200()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/pecas");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPecasSemEstoque_ComToken_DeveRetornar200()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/pecas/sem-estoque");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostPeca_ComDadosValidos_DeveRetornar201()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/pecas", new
            {
                codigo = "OL-INT-001",
                nome = "Óleo 5W30 Integração",
                precoUnitario = 45.90,
                quantidadeEstoque = 20
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostPeca_CodigoDuplicado_DeveRetornar400()
        {
            await AutenticarAsync();

            await _client.PostAsJsonAsync("/api/pecas", new
            {
                codigo = "OL-DUP-001",
                nome = "Óleo Duplicado",
                precoUnitario = 45.90,
                quantidadeEstoque = 10
            });

            var response = await _client.PostAsJsonAsync("/api/pecas", new
            {
                codigo = "OL-DUP-001",
                nome = "Óleo Duplicado 2",
                precoUnitario = 50.00,
                quantidadeEstoque = 5
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FluxoCRUD_Peca_DeveRealizarTodasAsOperacoes()
        {
            await AutenticarAsync();

            // POST
            var criarResponse = await _client.PostAsJsonAsync("/api/pecas", new
            {
                codigo = "FR-CRUD-001",
                nome = "Pastilha de Freio",
                precoUnitario = 89.90,
                quantidadeEstoque = 10
            });
            Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);
            var criada = await criarResponse.Content.ReadFromJsonAsync<IdResponse>();

            // GET por ID
            var getResponse = await _client.GetAsync($"/api/pecas/{criada!.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // PATCH estoque entrada
            var entradaResponse = await _client.PatchAsJsonAsync($"/api/pecas/{criada.Id}/estoque", new
            {
                tipo = "entrada",
                quantidade = 5
            });
            Assert.Equal(HttpStatusCode.OK, entradaResponse.StatusCode);
            var aposEntrada = await entradaResponse.Content.ReadFromJsonAsync<PecaResponse>();
            Assert.Equal(15, aposEntrada!.QuantidadeEstoque);

            // PATCH estoque saida
            var saidaResponse = await _client.PatchAsJsonAsync($"/api/pecas/{criada.Id}/estoque", new
            {
                tipo = "saida",
                quantidade = 3
            });
            Assert.Equal(HttpStatusCode.OK, saidaResponse.StatusCode);
            var aposSaida = await saidaResponse.Content.ReadFromJsonAsync<PecaResponse>();
            Assert.Equal(12, aposSaida!.QuantidadeEstoque);

            // PUT
            var putResponse = await _client.PutAsJsonAsync($"/api/pecas/{criada.Id}", new
            {
                nome = "Pastilha de Freio Premium",
                precoUnitario = 99.90
            });
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            // DELETE
            var deleteResponse = await _client.DeleteAsync($"/api/pecas/{criada.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task PatchEstoque_TipoInvalido_DeveRetornar400()
        {
            await AutenticarAsync();

            var criarResponse = await _client.PostAsJsonAsync("/api/pecas", new
            {
                codigo = "OL-ERR-001",
                nome = "Óleo Erro",
                precoUnitario = 45.00,
                quantidadeEstoque = 5
            });
            var criada = await criarResponse.Content.ReadFromJsonAsync<IdResponse>();

            var response = await _client.PatchAsJsonAsync($"/api/pecas/{criada!.Id}/estoque", new
            {
                tipo = "invalido",
                quantidade = 3
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPeca_NaoExistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync($"/api/pecas/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private sealed record IdResponse(Guid Id);
        private sealed record PecaResponse(Guid Id, string Codigo, string Nome, decimal PrecoUnitario, int QuantidadeEstoque);
    }

    public class ServicosIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public ServicosIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
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
        public async Task GetServicos_SemToken_DeveRetornar401()
        {
            var response = await _client.GetAsync("/api/servicos");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetServicos_ComToken_DeveRetornar200()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/servicos");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostServico_ComDadosValidos_DeveRetornar201()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Troca de Óleo Integração",
                descricao = "Troca completa de óleo do motor",
                preco = 150.00
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostServico_NomeDuplicado_DeveRetornar400()
        {
            await AutenticarAsync();

            await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Alinhamento Duplicado",
                descricao = "Alinhamento das rodas",
                preco = 80.00
            });

            var response = await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Alinhamento Duplicado",
                descricao = "Alinhamento novamente",
                preco = 90.00
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FluxoCRUD_Servico_DeveRealizarTodasAsOperacoes()
        {
            await AutenticarAsync();

            // POST
            var criarResponse = await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Balanceamento CRUD",
                descricao = "Balanceamento das rodas",
                preco = 60.00
            });
            Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);
            var criado = await criarResponse.Content.ReadFromJsonAsync<IdResponse>();

            // GET por ID
            var getResponse = await _client.GetAsync($"/api/servicos/{criado!.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // PUT
            var putResponse = await _client.PutAsJsonAsync($"/api/servicos/{criado.Id}", new
            {
                nome = "Balanceamento CRUD Premium",
                descricao = "Balanceamento premium das rodas",
                preco = 75.00
            });
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            // DELETE
            var deleteResponse = await _client.DeleteAsync($"/api/servicos/{criado.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task GetServico_NaoExistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync($"/api/servicos/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutServico_NomeDuplicadoDeOutro_DeveRetornar400()
        {
            await AutenticarAsync();

            await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Serviço A para PUT",
                descricao = "Desc",
                preco = 100.00
            });

            var criarB = await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Serviço B para PUT",
                descricao = "Desc",
                preco = 100.00
            });
            var b = await criarB.Content.ReadFromJsonAsync<IdResponse>();

            var putResponse = await _client.PutAsJsonAsync($"/api/servicos/{b!.Id}", new
            {
                nome = "Serviço A para PUT",
                descricao = "Desc",
                preco = 100.00
            });

            Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
        }

        private sealed record IdResponse(Guid Id);
    }
}
