namespace OficinaMecanica.Infrastructure.Settings
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 1025;
        public string RemetenteEmail { get; set; } = string.Empty;
        public string RemetenteNome { get; set; } = string.Empty;
    }
}
