using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public WorkOrderRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.WorkOrders
                .Include(w => w.Services)
                .Include(w => w.Parts)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }

        public async Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
        {
            return await _context.WorkOrders
                .AsNoTracking()
                .Include(w => w.Services)
                .Include(w => w.Parts)
                .FirstOrDefaultAsync(w => w.Number == number, cancellationToken);
        }

        public async Task<IEnumerable<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.WorkOrders
                .AsNoTracking()
                .Include(w => w.Services)
                .Include(w => w.Parts)
                .ToListAsync(cancellationToken);
        }

        public async Task<WorkOrder> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
        {
            await _context.WorkOrders.AddAsync(workOrder, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return workOrder;
        }

        public async Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.WorkOrders
                .AsNoTracking()
                .AnyAsync(w => w.Id == id, cancellationToken);
        }
    }
}
