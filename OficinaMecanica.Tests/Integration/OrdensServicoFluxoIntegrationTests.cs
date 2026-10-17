using OficinaMecanica.Contracts;
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

        /// <summary>Espera a Saga (em memória nos testes) processar as mensagens e a OS chegar ao status esperado.</summary>
        private async Task AguardarStatusAsync(Guid osId, string status)
        {
            string? atual = null;
            for (var i = 0; i < 50; i++)
            {
                atual = (await (await _client.GetAsync($"/api/ordens-servico/{osId}")).Content.ReadFromJsonAsync<StatusResponse>())!.Status;
                if (atual == status) return;
                await Task.Delay(100);
            }
            Assert.Fail($"OS {osId} ficou em {atual}, esperado {status}");
        }

        [Fact]
        public async Task FluxoCompleto_OS_PassandoPelaSagaAteEntregue()
        {
            await AutenticarAsync();

            var clienteId = await CriarClienteAsync("111.444.777-35", "Cliente Fluxo");
            var veiculoId = await CriarVeiculoAsync(clienteId, "FLX-0001");
            var servicoId = await CriarServicoAsync("Revisão Fluxo");
            var pecaId = await CriarPecaAsync("FLX-001", "Filtro Fluxo", 20);

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new { clienteId, veiculoId, observacoes = "Revisão completa" });
            Assert.Equal(HttpStatusCode.Created, osResponse.StatusCode);
            var osId = (await osResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/ordens-servico/{osId}/servicos", new { servicoId, quantidade = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/ordens-servico/{osId}/pecas", new { pecaId, quantidade = 2 })).StatusCode);

            // Diagnóstico concluído -> Saga pede orçamento ao Billing (202: processamento assíncrono)
            Assert.Equal(HttpStatusCode.Accepted, (await _client.PostAsync($"/api/ordens-servico/{osId}/concluir-diagnostico", null)).StatusCode);

            // Billing responde -> OS aguarda aprovação
            await _factory.PublicarAsync(new OrcamentoGerado(osId, Guid.NewGuid(), 200m));
            await AguardarStatusAsync(osId, "AguardandoAprovacao");

            var numero = (await (await _client.GetAsync($"/api/ordens-servico/{osId}")).Content.ReadFromJsonAsync<NumeroResponse>())!.Numero;
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/acompanhamento/{numero}/aprovar", null)).StatusCode);
            await AguardarStatusAsync(osId, "AguardandoPagamento");

            // Pagamento confirmado (Mercado Pago via Billing) -> OS vai para a fila de execução
            await _factory.PublicarAsync(new PagamentoAprovado(osId, Guid.NewGuid(), 200m));
            await AguardarStatusAsync(osId, "EmExecucao");

            // Execution Service concluiu -> OS finalizada
            await _factory.PublicarAsync(new ExecucaoFinalizada(osId, DateTime.UtcNow));
            await AguardarStatusAsync(osId, "Finalizada");

            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/ordens-servico/{osId}/registrar-entrega", null)).StatusCode);
            await AguardarStatusAsync(osId, "Entregue");
        }

        [Fact]
        public async Task Saga_FalhaNaExecucao_DeveEstornarPagamentoECancelarOs()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("390.533.447-05", "Cliente Estorno");
            var veiculoId = await CriarVeiculoAsync(clienteId, "EST-0002");
            var servicoId = await CriarServicoAsync("Servico Estorno");

            var osId = (await (await _client.PostAsJsonAsync("/api/ordens-servico", new { clienteId, veiculoId })).Content.ReadFromJsonAsync<IdResponse>())!.Id;
            await _client.PostAsJsonAsync($"/api/ordens-servico/{osId}/servicos", new { servicoId, quantidade = 1 });
            await _client.PostAsync($"/api/ordens-servico/{osId}/concluir-diagnostico", null);
            await _factory.PublicarAsync(new OrcamentoGerado(osId, Guid.NewGuid(), 100m));
            await AguardarStatusAsync(osId, "AguardandoAprovacao");

            var numero = (await (await _client.GetAsync($"/api/ordens-servico/{osId}")).Content.ReadFromJsonAsync<NumeroResponse>())!.Numero;
            await _client.PostAsync($"/api/acompanhamento/{numero}/aprovar", null);
            await AguardarStatusAsync(osId, "AguardandoPagamento");
            await _factory.PublicarAsync(new PagamentoAprovado(osId, Guid.NewGuid(), 100m));
            await AguardarStatusAsync(osId, "EmExecucao");

            // Falha na execução -> compensação: estorno -> OS cancelada
            // Reenvia os dois eventos até a Saga avançar: no teste eles podem chegar antes de a Saga gravar o
            // estado anterior (em produção a ordem é garantida por causalidade). Duplicatas são ignoradas.
            for (var i = 0; i < 30; i++)
            {
                await _factory.PublicarAsync(new ExecucaoFalhou(osId, "Peça indisponível"));
                await _factory.PublicarAsync(new PagamentoEstornado(osId, Guid.NewGuid()));
                await Task.Delay(100);
            }
            await AguardarStatusAsync(osId, "Cancelada");
        }

        [Fact]
        public async Task ConcluirDiagnostico_SemItens_DeveRetornar400()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("147.258.369-82", "Cliente Status Inv");
            var veiculoId = await CriarVeiculoAsync(clienteId, "STV-0001");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId
            });
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();

            // Tentar concluir diagn�stico sem itens (transi��o inv�lida)
            var response = await _client.PostAsync($"/api/ordens-servico/{os!.Id}/concluir-diagnostico", null);
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

            var response = await _client.PostAsJsonAsync($"/api/ordens-servico/{os!.Id}/cancelar", new
            {
                motivo = "Cliente desistiu do servi�o"
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
        public async Task Acompanhamento_SemToken_DeveRetornar401()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("951.462.873-09", "Cliente Acompanhamento");
            var veiculoId = await CriarVeiculoAsync(clienteId, "PUB-0001");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId, veiculoId
            });
            var os = await osResponse.Content.ReadFromJsonAsync<OsResponse>();

            _client.DefaultRequestHeaders.Authorization = null;
            var response = await _client.GetAsync($"/api/acompanhamento/{os!.Numero}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Acompanhamento_ComTokenDoProprioCliente_DeveRetornar200()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("111.222.333-96", "Cliente Dono Da OS");
            var veiculoId = await CriarVeiculoAsync(clienteId, "OWN-0001");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new { clienteId, veiculoId });
            var os = await osResponse.Content.ReadFromJsonAsync<OsResponse>();

            var tokenCliente = _factory.GerarTokenCliente(clienteId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCliente);

            var response = await _client.GetAsync($"/api/acompanhamento/{os!.Numero}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Acompanhamento_ComTokenDeOutroCliente_DeveRetornar403()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("444.555.666-19", "Cliente Dono Real");
            var veiculoId = await CriarVeiculoAsync(clienteId, "OWN-0002");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new { clienteId, veiculoId });
            var os = await osResponse.Content.ReadFromJsonAsync<OsResponse>();

            var tokenDeOutroCliente = _factory.GerarTokenCliente(Guid.NewGuid());
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDeOutroCliente);

            var response = await _client.GetAsync($"/api/acompanhamento/{os!.Numero}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListarOrdensServico_ComTokenDeCliente_DeveRetornar403()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("315.746.982-07", "Cliente Sem Acesso Staff");

            var tokenCliente = _factory.GerarTokenCliente(clienteId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCliente);

            var response = await _client.GetAsync("/api/ordens-servico");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListarClientes_ComTokenDeCliente_DeveRetornar403()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("958.634.271-53", "Outro Cliente Sem Acesso Staff");

            var tokenCliente = _factory.GerarTokenCliente(clienteId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCliente);

            var response = await _client.GetAsync("/api/clientes");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Aprovar_ComTokenAdministrativo_DeveRetornar200()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("753.951.486-80", "Cliente Aprovar");
            var veiculoId = await CriarVeiculoAsync(clienteId, "PUB-0002");
            var servicoId = await CriarServicoAsync("Servico Aprovar");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new { clienteId, veiculoId });
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();

            await _client.PostAsJsonAsync($"/api/ordens-servico/{os!.Id}/servicos", new { servicoId, quantidade = 1 });
            await _client.PostAsync($"/api/ordens-servico/{os.Id}/concluir-diagnostico", null);
            await _factory.PublicarAsync(new OrcamentoGerado(os.Id, Guid.NewGuid(), 100m));
            await AguardarStatusAsync(os.Id, "AguardandoAprovacao");
            var osNumero = await (await _client.GetAsync($"/api/ordens-servico/{os.Id}")).Content.ReadFromJsonAsync<NumeroResponse>();

            var response = await _client.PostAsync($"/api/acompanhamento/{osNumero!.Numero}/aprovar", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Reprovar_ComTokenAdministrativo_DeveRetornar200()
        {
            await AutenticarAsync();
            var clienteId = await CriarClienteAsync("428.619.375-64", "Cliente Reprovar");
            var veiculoId = await CriarVeiculoAsync(clienteId, "PUB-0003");
            var servicoId = await CriarServicoAsync("Servico Reprovar");

            var osResponse = await _client.PostAsJsonAsync("/api/ordens-servico", new { clienteId, veiculoId });
            var os = await osResponse.Content.ReadFromJsonAsync<IdResponse>();

            await _client.PostAsJsonAsync($"/api/ordens-servico/{os!.Id}/servicos", new { servicoId, quantidade = 1 });
            await _client.PostAsync($"/api/ordens-servico/{os.Id}/concluir-diagnostico", null);
            var osNumero = await (await _client.GetAsync($"/api/ordens-servico/{os.Id}")).Content.ReadFromJsonAsync<NumeroResponse>();

            var response = await _client.PostAsJsonAsync($"/api/acompanhamento/{osNumero!.Numero}/reprovar", new { motivo = "Valor acima do esperado" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    internal record OsResponse(Guid Id, string Numero);
    internal record StatusResponse(string Status);
}
