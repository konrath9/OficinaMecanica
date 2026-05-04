namespace OficinaMecanica.Application.DTOs.Clientes
{
    public class AtualizarClienteRequest
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Documento { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
    }
}
