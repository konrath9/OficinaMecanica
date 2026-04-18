using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IClienteRepository
    {
        Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Cliente?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default);
        Task<IEnumerable<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
        Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
