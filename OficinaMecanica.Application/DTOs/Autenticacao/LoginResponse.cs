namespace OficinaMecanica.Application.DTOs.Autenticacao
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string NomeUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
    }
}
