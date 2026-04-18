namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class TempoMedioExecucaoResponse
    {
        public double TempoMedioHoras { get; set; }
        public double TempoMedioDias { get; set; }
        public int TotalOrdensFinalizadas { get; set; }
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFim { get; set; }
    }
}
