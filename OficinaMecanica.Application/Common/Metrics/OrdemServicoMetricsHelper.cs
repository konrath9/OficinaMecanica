using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.Common.Metrics
{
    /// <summary>
    /// Calcula a duracao entre duas entradas do historico de status da OS, usada pelos use cases
    /// para alimentar a metrica ordens_servico.tempo_por_status_segundos.
    /// </summary>
    public static class OrdemServicoMetricsHelper
    {
        public static TimeSpan? DuracaoEntre(
            IEnumerable<HistoricoStatusOrdemServico> historico,
            StatusOrdemServico inicio,
            StatusOrdemServico fim)
        {
            var ocorridoEmInicio = historico.FirstOrDefault(h => h.Status == inicio)?.OcorridoEm;
            var ocorridoEmFim = historico.FirstOrDefault(h => h.Status == fim)?.OcorridoEm;

            if (!ocorridoEmInicio.HasValue || !ocorridoEmFim.HasValue)
                return null;

            return ocorridoEmFim.Value - ocorridoEmInicio.Value;
        }
    }
}
