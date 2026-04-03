using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class PartRepository : IPartRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public PartRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Parts
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Parts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == code.ToUpperInvariant(), cancellationToken);
        }

        public async Task<IEnumerable<Part>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Parts
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Part>> GetLowStockAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Parts
                .AsNoTracking()
                .Where(p => p.StockQuantity == 0)
                .ToListAsync(cancellationToken);
        }

        public async Task<Part> AddAsync(Part part, CancellationToken cancellationToken = default)
        {
            await _context.Parts.AddAsync(part, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return part;
        }

        public async Task UpdateAsync(Part part, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var part = await _context.Parts.FindAsync(new object[] { id }, cancellationToken);
            if (part != null)
            {
                _context.Parts.Remove(part);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Parts
                .AsNoTracking()
                .AnyAsync(p => p.Id == id, cancellationToken);
        }
    }
}
