using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Domain.ValueObjects
{
    /// <summary>
    /// Registra cada mudança de status da OS para auditoria e acompanhamento.
    /// </summary>
    public class HistoricoStatusOrdemServico
    {
        public StatusOrdemServico Status { get; }
        public DateTime OcorridoEm { get; }
        public string? Observacao { get; }

        public HistoricoStatusOrdemServico(StatusOrdemServico status, string? observacao = null)
        {
            Status = status;
            OcorridoEm = DateTime.UtcNow;
            Observacao = observacao;
        }

        // Para EF Core
        private HistoricoStatusOrdemServico() { }
    }
}
