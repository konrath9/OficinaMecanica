namespace OficinaMecanica.Application.Interfaces.Services
{
    public interface ISenhaService
    {
        string Criptografar(string senha);
        bool Verificar(string senha, string hash);
    }
}
