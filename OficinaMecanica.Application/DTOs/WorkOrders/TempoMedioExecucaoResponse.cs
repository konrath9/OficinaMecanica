namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class TempoMedioExecucaoResponse
    {
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFim { get; set; }
        public int TotalServicosFinalizados { get; set; }
        public IEnumerable<TempoMedioPorServicoDto> PorServico { get; set; } = [];
    }

    public class TempoMedioPorServicoDto
    {
        public Guid ServicoId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public double TempoMedioHoras { get; set; }
        public double TempoMedioDias { get; set; }
        public int TotalExecucoes { get; set; }
    }
}
