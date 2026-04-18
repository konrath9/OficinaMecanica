namespace OficinaMecanica.Application.DTOs.Clientes
{
    public class CriarClienteRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
    }
}
