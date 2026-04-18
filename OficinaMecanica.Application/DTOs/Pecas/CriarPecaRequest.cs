namespace OficinaMecanica.Application.DTOs.Pecas
{
    public class CriarPecaRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int QuantidadeEstoque { get; set; }
    }
}
