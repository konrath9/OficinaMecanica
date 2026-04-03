using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public ServiceRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Service?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        }

        public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Services
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Service> AddAsync(Service service, CancellationToken cancellationToken = default)
        {
            await _context.Services.AddAsync(service, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return service;
        }

        public async Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var service = await _context.Services.FindAsync(new object[] { id }, cancellationToken);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Services
                .AsNoTracking()
                .AnyAsync(s => s.Id == id, cancellationToken);
        }
    }
}
