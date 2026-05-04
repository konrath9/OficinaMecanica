namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AdicionarPecaOrdemServicoResponse
    {
        public Guid OrdemServicoId { get; set; }

        public AdicionarPecaOrdemServicoResponse(Guid ordemServicoId)
        {
            OrdemServicoId = ordemServicoId;
        }
    }
}
