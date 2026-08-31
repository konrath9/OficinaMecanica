using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    /// <summary>
    /// Resposta p�blica para acompanhamento da OS pelo cliente (sem dados sens�veis)
    /// </summary>
    public class AcompanhamentoOrdemServicoResponse
    {
        public Guid OrdemServicoId { get; set; }
        public Guid ClienteId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public StatusOrdemServico Status { get; set; }
        public string StatusDescricao { get; set; } = string.Empty;
        public string MensagemStatus { get; set; } = string.Empty;
        public string DescricaoVeiculo { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public decimal TotalOrcamento { get; set; }
        public DateTime CriadaEm { get; set; }
        public DateTime? IniciadaEm { get; set; }
        public DateTime? FinalizadaEm { get; set; }
        public DateTime? EntregueEm { get; set; }
        public ProgressoServicosResponse Progresso { get; set; } = new();
        public IReadOnlyList<HistoricoStatusResponse> Historico { get; set; } = [];
    }

    public class ProgressoServicosResponse
    {
        public int Concluidos { get; set; }
        public int Total { get; set; }
        public int PercentualConcluido => Total == 0 ? 0 : (int)Math.Round((double)Concluidos / Total * 100);
    }

    public class HistoricoStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime OcorridoEm { get; set; }
        public string? Observacao { get; set; }
    }
}

