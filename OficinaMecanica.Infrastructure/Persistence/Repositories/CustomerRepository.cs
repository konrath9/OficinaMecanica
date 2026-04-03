using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id != Guid.Empty);
        }
    }
}
