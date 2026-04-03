using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IServiceRepository
    {
        Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Service?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Service> AddAsync(Service service, CancellationToken cancellationToken = default);
        Task UpdateAsync(Service service, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
