using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    /// <summary>
    /// Resposta pública para acompanhamento da OS pelo cliente (sem dados sensíveis)
    /// </summary>
    public class AcompanhamentoOrdemServicoResponse
    {
        public string Numero { get; set; } = string.Empty;
        public StatusOrdemServico Status { get; set; }
        public string StatusDescricao { get; set; } = string.Empty;
        public string DescricaoVeiculo { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public decimal TotalOrcamento { get; set; }
        public DateTime CriadaEm { get; set; }
        public DateTime? IniciadaEm { get; set; }
        public DateTime? FinalizadaEm { get; set; }
        public DateTime? EntregueEm { get; set; }
    }
}
