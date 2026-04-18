using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class VeiculoRepository : IVeiculoRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public VeiculoRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Veiculos.Include(v => v.Cliente).FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        public async Task<Veiculo?> GetByPlacaAsync(string placa, CancellationToken cancellationToken = default)
            => await _context.Veiculos.AsNoTracking().Include(v => v.Cliente).FirstOrDefaultAsync(v => v.Placa == placa.ToUpperInvariant(), cancellationToken);

        public async Task<IEnumerable<Veiculo>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Veiculos.AsNoTracking().Include(v => v.Cliente).ToListAsync(cancellationToken);

        public async Task<IEnumerable<Veiculo>> GetByClienteIdAsync(Guid clienteId, CancellationToken cancellationToken = default)
            => await _context.Veiculos.AsNoTracking().Where(v => v.ClienteId == clienteId).ToListAsync(cancellationToken);

        public async Task<Veiculo> AddAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
        {
            await _context.Veiculos.AddAsync(veiculo, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return veiculo;
        }

        public async Task UpdateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var veiculo = await _context.Veiculos.FindAsync(new object[] { id }, cancellationToken);
            if (veiculo != null)
            {
                _context.Veiculos.Remove(veiculo);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Veiculos.AsNoTracking().AnyAsync(v => v.Id == id, cancellationToken);
    }
}

