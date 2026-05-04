using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Integration
{
    public class OrdensServicoFluxoIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly OficinaMecanicaWebApplicationFactory _factory;

        public OrdensServicoFluxoIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
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

        private async Task<Guid> CriarClienteAsync(string documento, string nome)
        {
            var r = await _client.PostAsJsonAsync("/api/clientes", new { nome, documento });
            return (await r.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        }

        private async Task<Guid> CriarVeiculoAsync(Guid clienteId, string placa)
        {
            var r = await _client.PostAsJsonAsync("/api/veiculos", new
            {
                placa, marca = "Toyota", modelo = "Corolla", ano = 2020, clienteId
            });
            return (await r.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        }

        private async Task<Guid> CriarServicoAsync(string nome)
        {
            var r = await _client.PostAsJsonAsync("/api/servicos", new
            {
                nome, descricao = "Desc", preco = 100.00
            });
            return (await r.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        }

        private async Task<Guid> CriarPecaAsync(string codigo, string nome, int estoque = 20)
        {
            var r = await _client.PostAsJsonAsync("/api/pecas", new
            {
                codigo, nome, precoUnitario = 50.00, quantidadeEstoque = estoque
            });
            return (await r.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        }

        [Fact]
        public async Task CriarOS_ComDadosValidos_DeveRetornar201()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("529.982.247-25", "Cliente OS");
            var veiculoId = await CriarVeiculoAsync(clienteId, "OST-0001");

            var response = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId,
                veiculoId,
                observacoes = "Barulho no motor"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CriarOS_ClienteInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId = Guid.NewGuid(),
                veiculoId = Guid.NewGuid()
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetOS_IdInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync($"/api/ordens-servico/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TempoMedioExecucao_DeveRetornar200()
        {
            await AutenticarAsync();
            var response = await _client.GetAsync("/api/ordens-servico/relatorios/tempo-medio-execucao");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task FluxoCompleto_OS_AdicionarServicoPecaEAvancarStatus()
        {
            await AutenticarAsync();

            var clienteId = await CriarClienteAsync("111.444.777-35", "Cliente Fluxo");
            var veiculoId = await CriarVeiculoAsync(clienteId, "FLX-0001");
            var servicoId = await CriarServicoAsync("Revisão Fluxo");
            var pecaId = await CriarPecaAsync("FLX-001", "Filtro Fluxo", 20);

            // Criar OS
            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId, observacoes = "Revisão completa"
            });
            Assert.Equal(HttpStatusCode.Created, osResponse.StatusCode);
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();
            var osId = os!.Id;

            // Adicionar serviço
            var addServico = await _client.PostAsJsonAsync($"/api/ordens-servico/{osId}/servicos", new
            {
                servicoId, quantidade = 1
            });
            Assert.Equal(HttpStatusCode.OK, addServico.StatusCode);

            // Adicionar peça
            var addPeca = await _client.PostAsJsonAsync($"/api/ordens-servico/{osId}/pecas", new
            {
                pecaId, quantidade = 2
            });
            Assert.Equal(HttpStatusCode.OK, addPeca.StatusCode);

            // Iniciar Diagnóstico
            var iniciar = await _client.PutAsJsonAsync($"/api/ordens-servico/{osId}/status", new
            {
                acao = 1 // IniciarDiagnostico
            });
            Assert.Equal(HttpStatusCode.OK, iniciar.StatusCode);

            // Enviar para Aprovação
            var aprovar = await _client.PutAsJsonAsync($"/api/ordens-servico/{osId}/status", new
            {
                acao = 2 // EnviarParaAprovacao
            });
            Assert.Equal(HttpStatusCode.OK, aprovar.StatusCode);

            // Aprovar
            var executar = await _client.PutAsJsonAsync($"/api/ordens-servico/{osId}/status", new
            {
                acao = 3 // Aprovar
            });
            Assert.Equal(HttpStatusCode.OK, executar.StatusCode);

            // Finalizar
            var finalizar = await _client.PutAsJsonAsync($"/api/ordens-servico/{osId}/status", new
            {
                acao = 4 // Finalizar
            });
            Assert.Equal(HttpStatusCode.OK, finalizar.StatusCode);

            // Entregar
            var entregar = await _client.PutAsJsonAsync($"/api/ordens-servico/{osId}/status", new
            {
                acao = 5 // Entregar
            });
            Assert.Equal(HttpStatusCode.OK, entregar.StatusCode);

            // GET para confirmar estado final
            var get = await _client.GetAsync($"/api/ordens-servico/{osId}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }

        [Fact]
        public async Task AlterarStatus_StatusInvalido_DeveRetornar400()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("147.258.369-82", "Cliente Status Inv");
            var veiculoId = await CriarVeiculoAsync(clienteId, "STV-0001");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId
            });
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();

            // Tentar entregar direto (transição inválida)
            var response = await _client.PutAsJsonAsync($"/api/ordens-servico/{os!.Id}/status", new
            {
                acao = 5 // Entregar sem passar pelas etapas
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CancelarOS_DeveRetornar200()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("258.369.147-37", "Cliente Cancelar");
            var veiculoId = await CriarVeiculoAsync(clienteId, "CAN-0001");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId
            });
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();

            var response = await _client.PutAsJsonAsync($"/api/ordens-servico/{os!.Id}/status", new
            {
                acao = 6 // Cancelar
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AdicionarServico_OSInexistente_DeveRetornar404()
        {
            await AutenticarAsync();
            var servicoId = await CriarServicoAsync("Servico Inexistente");

            var response = await _client.PostAsJsonAsync($"/api/ordens-servico/{Guid.NewGuid()}/servicos", new
            {
                servicoId, quantidade = 1
            });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AdicionarPeca_EstoqueInsuficiente_DeveRetornar400()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("369.147.258-37", "Cliente Estoque");
            var veiculoId = await CriarVeiculoAsync(clienteId, "EST-0001");
            var pecaId = await CriarPecaAsync("EST-001", "Peca Estoque Baixo", 1);

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId
            });
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();

            var response = await _client.PostAsJsonAsync($"/api/ordens-servico/{os!.Id}/pecas", new
            {
                pecaId, quantidade = 99
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AcompanhamentoPublico_OSExistente_DeveRetornar200SemToken()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("951.462.873-09", "Cliente Publico");
            var veiculoId = await CriarVeiculoAsync(clienteId, "PUB-0001");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId
            });
            var os = await osResponse.Content.ReadFromJsonAsync<OsResponse>();

            // Remover token e consultar endpoint público
            _client.DefaultRequestHeaders.Authorization = null;
            var response = await _client.GetAsync($"/api/publico/acompanhamento/{os!.Numero}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    internal record OsResponse(Guid Id, string Numero);
}
