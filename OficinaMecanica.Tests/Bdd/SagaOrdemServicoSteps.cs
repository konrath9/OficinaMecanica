using OficinaMecanica.Contracts;
using OficinaMecanica.Tests.Integration;
using Reqnroll;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OficinaMecanica.Tests.Bdd
{
    /// <summary>
    /// Passos do BDD da Saga. Sobe a API real (WebApplicationFactory) com a state machine rodando em memória;
    /// Billing e Execution são simulados publicando no bus os mesmos eventos que eles publicariam via RabbitMQ.
    /// </summary>
    [Binding]
    public sealed class SagaOrdemServicoSteps : IDisposable
    {
        private readonly OficinaMecanicaWebApplicationFactory _factory = new();
        private readonly HttpClient _client;
        private Guid _osId;
        private string _numero = string.Empty;

        public SagaOrdemServicoSteps()
        {
            _client = _factory.CreateClient();
        }

        [Given("uma OS com um serviço e uma peça em diagnóstico")]
        public async Task DadoUmaOsEmDiagnostico()
        {
            await _factory.SeedAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _factory.ObterTokenAsync(_client));

            var clienteId = await CriarAsync("/api/clientes", new { nome = "Cliente BDD", documento = "529.982.247-25", email = "cliente@bdd.com" });
            var veiculoId = await CriarAsync("/api/veiculos", new { placa = "BDD-0001", marca = "Fiat", modelo = "Uno", ano = 2015, clienteId });
            var servicoId = await CriarAsync("/api/servicos", new { nome = "Troca de embreagem", descricao = "Kit completo", preco = 800.00 });
            var pecaId = await CriarAsync("/api/pecas", new { codigo = "EMB-01", nome = "Kit embreagem", precoUnitario = 450.00, quantidadeEstoque = 5 });

            _osId = await CriarAsync("/api/ordens-servico", new { clienteId, veiculoId, observacoes = "Embreagem patinando" });
            (await _client.PostAsJsonAsync($"/api/ordens-servico/{_osId}/servicos", new { servicoId, quantidade = 1 })).EnsureSuccessStatusCode();
            (await _client.PostAsJsonAsync($"/api/ordens-servico/{_osId}/pecas", new { pecaId, quantidade = 1 })).EnsureSuccessStatusCode();

            _numero = (await _client.GetFromJsonAsync<NumeroResponse>($"/api/ordens-servico/{_osId}"))!.Numero;
            await AguardarStatusAsync("EmDiagnostico");
        }

        [When("o técnico conclui o diagnóstico")]
        public async Task QuandoConcluiDiagnostico() =>
            Assert.Equal(HttpStatusCode.Accepted, (await _client.PostAsync($"/api/ordens-servico/{_osId}/concluir-diagnostico", null)).StatusCode);

        [When("o Billing Service gera o orçamento")]
        public async Task QuandoBillingGeraOrcamento()
        {
            await _factory.PublicarAsync(new OrcamentoGerado(_osId, Guid.NewGuid(), 1250m));
            await AguardarStatusAsync("AguardandoAprovacao");
        }

        [When("o Billing Service falha ao gerar o orçamento")]
        public Task QuandoBillingFalha() =>
            _factory.PublicarAsync(new OrcamentoFalhou(_osId, "Valor do orçamento inválido"));

        [When("o cliente aprova o orçamento")]
        public async Task QuandoClienteAprova() =>
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/acompanhamento/{_numero}/aprovar", null)).StatusCode);

        [When("o Mercado Pago confirma o pagamento")]
        public Task QuandoPagamentoAprovado() =>
            PublicarAteStatusAsync(new PagamentoAprovado(_osId, Guid.NewGuid(), 1250m), "EmExecucao");

        [When("o Mercado Pago recusa o pagamento")]
        public Task QuandoPagamentoRecusado() =>
            PublicarAteStatusAsync(new PagamentoRecusado(_osId, Guid.NewGuid(), "cc_rejected_insufficient_amount"), "Cancelada");

        [When("o Execution Service finaliza a execução")]
        public Task QuandoExecucaoFinalizada() =>
            _factory.PublicarAsync(new ExecucaoFinalizada(_osId, DateTime.UtcNow));

        [When("o Execution Service reporta falha na execução")]
        public Task QuandoExecucaoFalhou() =>
            _factory.PublicarAsync(new ExecucaoFalhou(_osId, "Peça com defeito de fábrica"));

        [When("o Billing Service confirma o estorno")]
        public Task QuandoEstornoConfirmado() =>
            PublicarAteStatusAsync(new PagamentoEstornado(_osId, Guid.NewGuid()), "Cancelada");

        [When("a recepção registra a entrega")]
        public async Task QuandoRegistraEntrega() =>
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/ordens-servico/{_osId}/registrar-entrega", null)).StatusCode);

        [Then("a OS deve estar {string}")]
        public Task EntaoOsDeveEstar(string status) => AguardarStatusAsync(status);

        [Then("o histórico da OS deve registrar a falha do orçamento")]
        public async Task EntaoHistoricoRegistraFalha()
        {
            for (var i = 0; i < 50; i++)
            {
                var os = await _client.GetFromJsonAsync<AcompanhamentoResponse>($"/api/acompanhamento/{_numero}");
                if (os!.Historico.Any(h => h.Observacao?.Contains("Falha ao gerar orçamento") == true))
                    return;
                await Task.Delay(100);
            }
            Assert.Fail("Histórico não registrou a falha do orçamento");
        }

        private async Task<Guid> CriarAsync(string url, object body)
        {
            var response = await _client.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        }

        /// <summary>
        /// Eventos de outros serviços chegam de forma assíncrona; reenviar até o efeito aparecer imita a
        /// entrega at-least-once do RabbitMQ (a Saga ignora duplicatas).
        /// </summary>
        private async Task PublicarAteStatusAsync<T>(T evento, string status) where T : class
        {
            for (var i = 0; i < 30; i++)
            {
                await _factory.PublicarAsync(evento);
                await Task.Delay(100);
                if (await StatusAtualAsync() == status) return;
            }
            Assert.Fail($"OS não chegou a {status} (atual: {await StatusAtualAsync()})");
        }

        private async Task AguardarStatusAsync(string status)
        {
            for (var i = 0; i < 50; i++)
            {
                if (await StatusAtualAsync() == status) return;
                await Task.Delay(100);
            }
            Assert.Fail($"OS não chegou a {status} (atual: {await StatusAtualAsync()})");
        }

        private async Task<string> StatusAtualAsync() =>
            (await _client.GetFromJsonAsync<StatusResponse>($"/api/ordens-servico/{_osId}"))!.Status;

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private sealed record AcompanhamentoResponse(List<HistoricoItem> Historico);
        private sealed record HistoricoItem(string Status, string? Observacao);
    }
}
