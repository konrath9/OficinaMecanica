using System.Diagnostics.Metrics;

namespace OficinaMecanica.Application.Common.Metrics
{
    /// <summary>
    /// Metricas de negocio da OS, expostas via System.Diagnostics.Metrics (API neutra de
    /// fornecedor - quem le e exporta essas metricas para o New Relic e o OpenTelemetry SDK
    /// configurado em OficinaMecanica.API/Program.cs, via AddMeter(MeterName)).
    /// </summary>
    public class OrdemServicoMetrics
    {
        public const string MeterName = "OficinaMecanica.OrdemServico";

        private readonly Counter<long> _ordensCriadas;
        private readonly Histogram<double> _tempoPorStatus;
        private readonly Counter<long> _errosIntegracao;

        public OrdemServicoMetrics()
        {
            var meter = new Meter(MeterName);

            _ordensCriadas = meter.CreateCounter<long>(
                "ordens_servico.criadas",
                description: "Total de Ordens de Servico criadas (suporta o dashboard de volume diario)");

            _tempoPorStatus = meter.CreateHistogram<double>(
                "ordens_servico.tempo_por_status_segundos",
                unit: "s",
                description: "Tempo gasto pela OS em cada status (Diagnostico, Execucao, Finalizacao)");

            _errosIntegracao = meter.CreateCounter<long>(
                "integracoes.erros",
                description: "Falhas em integracoes externas (ex.: envio de e-mail de notificacao)");
        }

        public void RegistrarOrdemCriada() => _ordensCriadas.Add(1);

        public void RegistrarTempoStatus(string status, TimeSpan duracao) =>
            _tempoPorStatus.Record(duracao.TotalSeconds, new KeyValuePair<string, object?>("status", status));

        public void RegistrarErroIntegracao(string integracao) =>
            _errosIntegracao.Add(1, new KeyValuePair<string, object?>("integracao", integracao));
    }
}
