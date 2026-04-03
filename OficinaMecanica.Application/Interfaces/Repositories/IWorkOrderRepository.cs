using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IWorkOrderRepository
    {
        Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<WorkOrder?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<WorkOrder> AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
        Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
