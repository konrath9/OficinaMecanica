namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    public class TempoMedioExecucaoResponse
    {
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFim { get; set; }
        public int TotalOrdensFinalizadas { get; set; }
        public double TempoMedioHoras { get; set; }
        public double TempoMedioDias { get; set; }
    }
}
