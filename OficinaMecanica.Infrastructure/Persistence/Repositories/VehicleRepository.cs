using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public VehicleRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles
                .Include(v => v.Customer)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .Include(v => v.Customer)
                .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate.ToUpperInvariant(), cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .Include(v => v.Customer)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.CustomerId == customerId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            await _context.Vehicles.AddAsync(vehicle, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return vehicle;
        }

        public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var vehicle = await _context.Vehicles.FindAsync(new object[] { id }, cancellationToken);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .AnyAsync(v => v.Id == id, cancellationToken);
        }
    }
}

