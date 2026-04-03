using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IPartRepository
    {
        Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<IEnumerable<Part>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Part>> GetLowStockAsync(CancellationToken cancellationToken = default);
        Task<Part> AddAsync(Part part, CancellationToken cancellationToken = default);
        Task UpdateAsync(Part part, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
