using System.Net;

namespace OficinaMecanica.Tests.Integration
{
    public class ObservabilidadeIntegrationTests : IClassFixture<OficinaMecanicaWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ObservabilidadeIntegrationTests(OficinaMecanicaWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_SemToken_DeveRetornar200()
        {
            var response = await _client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Requisicao_SemHeaderDeCorrelacao_DeveGerarUmNaResposta()
        {
            var response = await _client.GetAsync("/health");

            Assert.True(response.Headers.Contains("X-Correlation-Id"));
            var valor = response.Headers.GetValues("X-Correlation-Id").Single();
            Assert.False(string.IsNullOrWhiteSpace(valor));
        }

        [Fact]
        public async Task Requisicao_ComHeaderDeCorrelacao_DevePropagarOMesmoValor()
        {
            var meuId = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Correlation-Id", meuId);

            var response = await _client.SendAsync(request);

            Assert.Equal(meuId, response.Headers.GetValues("X-Correlation-Id").Single());
        }
    }
}
