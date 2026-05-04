namespace OficinaMecanica.Application.DTOs.Servicos
{
    public class AtualizarServicoRequest
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }
}
