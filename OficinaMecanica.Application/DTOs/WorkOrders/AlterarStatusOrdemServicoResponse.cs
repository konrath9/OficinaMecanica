namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AlterarStatusOrdemServicoResponse
    {
        public Guid OrdemServicoId { get; set; }

        public AlterarStatusOrdemServicoResponse(Guid ordemServicoId)
        {
            OrdemServicoId = ordemServicoId;
        }
    }
}
