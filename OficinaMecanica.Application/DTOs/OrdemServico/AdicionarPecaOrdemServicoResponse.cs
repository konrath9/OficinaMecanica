namespace OficinaMecanica.Application.DTOs.OrdemServico
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
