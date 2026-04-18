namespace OficinaMecanica.Application.DTOs.Veiculos
{
    public class AtualizarVeiculoRequest
    {
        public Guid Id { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Ano { get; set; }
    }
}
