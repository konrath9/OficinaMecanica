using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);
        Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default);
        Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
