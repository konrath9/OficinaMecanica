namespace OficinaMecanica.Application.DTOs.Servicos
{
    public class CriarServicoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }
}
