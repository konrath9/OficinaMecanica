using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Services
{
    public interface ITokenService
    {
        string GerarToken(Usuario usuario);
    }
}
