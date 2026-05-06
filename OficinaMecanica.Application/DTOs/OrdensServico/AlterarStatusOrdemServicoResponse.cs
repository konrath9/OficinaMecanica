namespace OficinaMecanica.Application.DTOs.OrdemServico
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
