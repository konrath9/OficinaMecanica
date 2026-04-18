namespace OficinaMecanica.Application.DTOs.Veiculos
{
    public class VeiculoResponse
    {
        public Guid Id { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Ano { get; set; }
        public Guid ClienteId { get; set; }
        public string? NomeCliente { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
