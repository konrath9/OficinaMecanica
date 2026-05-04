namespace OficinaMecanica.Application.DTOs.Pecas
{
    public class PecaResponse
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int QuantidadeEstoque { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
