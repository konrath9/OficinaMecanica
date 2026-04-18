namespace OficinaMecanica.Application.DTOs.Pecas
{
    public class AtualizarPecaRequest
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
    }
}
