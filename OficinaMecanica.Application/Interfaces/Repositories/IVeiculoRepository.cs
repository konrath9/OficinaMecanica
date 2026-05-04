using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IVeiculoRepository
    {
        Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Veiculo?> GetByPlacaAsync(string placa, CancellationToken cancellationToken = default);
        Task<IEnumerable<Veiculo>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Veiculo>> GetByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default);
        Task<Veiculo> AddAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
        Task UpdateAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}


