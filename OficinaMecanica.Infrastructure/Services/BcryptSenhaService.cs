using OficinaMecanica.Application.Interfaces.Services;

namespace OficinaMecanica.Infrastructure.Services
{
    public class BcryptSenhaService : ISenhaService
    {
        public string Criptografar(string senha)
            => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);

        public bool Verificar(string senha, string hash)
            => BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
