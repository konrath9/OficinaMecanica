using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    public class CriarOrdemServicoResponse
    {
        public Guid Id { get; set; }
        public string Numero { get; set; }
        public StatusOrdemServico Status { get; set; }
        public DateTime CriadaEm { get; set; }

        public CriarOrdemServicoResponse(Guid id, string numero, StatusOrdemServico status, DateTime criadaEm)
        {
            Id = id;
            Numero = numero;
            Status = status;
            CriadaEm = criadaEm;
        }
    }
}
