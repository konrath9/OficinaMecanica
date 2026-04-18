namespace OficinaMecanica.Application.DTOs.Veiculos
{
    public class VeiculoResponse
    {
        public Guid Id { get; set; }
        public string Placa { get; set; } = string.Empty;         // normalizada (ex: ABC1234)
        public string PlacaFormatada { get; set; } = string.Empty; // com hífen (ex: ABC-1234)
        public string FormatoPlaca { get; set; } = string.Empty;   // "Antigo" ou "Mercosul"
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Ano { get; set; }
        public Guid ClienteId { get; set; }
        public string? NomeCliente { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
