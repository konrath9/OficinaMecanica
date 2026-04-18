namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AdicionarPecaOrdemServicoRequest
    {
        public Guid OrdemServicoId { get; set; }
        public Guid PecaId { get; set; }
        public int Quantidade { get; set; }
    }
}

