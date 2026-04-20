using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default);
        Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default);
        Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
