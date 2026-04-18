using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IPecaRepository
    {
        Task<Peca?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Peca?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
        Task<IEnumerable<Peca>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Peca>> GetSemEstoqueAsync(CancellationToken cancellationToken = default);
        Task<Peca> AddAsync(Peca peca, CancellationToken cancellationToken = default);
        Task UpdateAsync(Peca peca, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
