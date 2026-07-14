using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    public class OrdemServicoResponse
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public StatusOrdemServico Status { get; set; }
        public string StatusDescricao { get; set; } = string.Empty;
        public Guid ClienteId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public Guid VeiculoId { get; set; }
        public string DescricaoVeiculo { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public decimal TotalServicos { get; set; }
        public decimal TotalPecas { get; set; }
        public decimal TotalOrcamento { get; set; }
        public DateTime CriadaEm { get; set; }
        public DateTime? IniciadaEm { get; set; }
        public DateTime? FinalizadaEm { get; set; }
        public DateTime? EntregueEm { get; set; }
    }
}
