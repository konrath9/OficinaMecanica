using OficinaMecanica.Domain.ValueObjects;

namespace OficinaMecanica.Application.DTOs.Clientes
{
    public class ClienteResponse
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;    // somente dígitos
        public string DocumentoFormatado { get; set; } = string.Empty; // com máscara
        public string TipoDocumento { get; set; } = string.Empty; // "CPF" ou "CNPJ"
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
