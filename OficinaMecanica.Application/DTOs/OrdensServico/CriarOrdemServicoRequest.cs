namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    public class CriarOrdemServicoRequest
    {
        public Guid ClienteId { get; set; }
        public Guid VeiculoId { get; set; }
        public string? Observacoes { get; set; }
    }
}
