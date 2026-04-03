using OficinaMecanica.Application.Interfaces.Repositories;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id != Guid.Empty);
        }
    }
}
