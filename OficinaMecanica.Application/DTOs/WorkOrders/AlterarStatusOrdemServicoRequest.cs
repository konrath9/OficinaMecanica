using OficinaMecanica.Application.Enums;

namespace OficinaMecanica.Application.DTOs.WorkOrders
{
    public class AlterarStatusOrdemServicoRequest
    {
        public Guid OrdemServicoId { get; set; }
        public AcaoOrdemServico Acao { get; set; }
    }
}
