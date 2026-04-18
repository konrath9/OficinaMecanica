using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IOrdemServicoRepository
    {
        Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OrdemServico?> GetByNumeroAsync(string numero, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrdemServico>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<OrdemServico>> GetFinalizadasNoPeriodoAsync(DateTime? inicio, DateTime? fim, CancellationToken cancellationToken = default);
        Task<OrdemServico> AddAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
        Task UpdateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
