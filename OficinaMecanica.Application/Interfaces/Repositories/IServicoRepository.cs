using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IServicoRepository
    {
        Task<Servico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Servico?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default);
        Task<IEnumerable<Servico>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Servico> AddAsync(Servico servico, CancellationToken cancellationToken = default);
        Task UpdateAsync(Servico servico, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
