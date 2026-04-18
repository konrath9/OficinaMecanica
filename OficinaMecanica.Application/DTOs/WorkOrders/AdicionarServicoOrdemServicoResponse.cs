namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AdicionarServicoOrdemServicoResponse
    {
        public Guid OrdemServicoId { get; set; }

        public AdicionarServicoOrdemServicoResponse(Guid ordemServicoId)
        {
            OrdemServicoId = ordemServicoId;
        }
    }
}
