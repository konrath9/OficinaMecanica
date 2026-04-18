namespace OficinaMecanica.Application.DTOs.Veiculos
{
    public class CriarVeiculoRequest
    {
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Ano { get; set; }
        public Guid ClienteId { get; set; }
    }
}
