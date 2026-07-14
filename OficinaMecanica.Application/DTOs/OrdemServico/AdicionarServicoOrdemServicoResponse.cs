namespace OficinaMecanica.Application.DTOs.OrdemServico
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
