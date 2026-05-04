namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AdicionarServicoOrdemServicoRequest
    {
        public Guid OrdemServicoId { get; set; }
        public Guid ServicoId { get; set; }
        public int Quantidade { get; set; }
    }
}

