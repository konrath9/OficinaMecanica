using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IVehicleRepository
    {
        Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Vehicle?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
        Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}


