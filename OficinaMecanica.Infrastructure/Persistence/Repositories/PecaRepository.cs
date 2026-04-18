using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class PecaRepository : IPecaRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public PecaRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Peca?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Pecas.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        public async Task<Peca?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default)
            => await _context.Pecas.AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == codigo.ToUpperInvariant(), cancellationToken);

        public async Task<IEnumerable<Peca>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Pecas.AsNoTracking().ToListAsync(cancellationToken);

        public async Task<IEnumerable<Peca>> GetSemEstoqueAsync(CancellationToken cancellationToken = default)
            => await _context.Pecas.AsNoTracking().Where(p => p.QuantidadeEstoque == 0).ToListAsync(cancellationToken);

        public async Task<Peca> AddAsync(Peca peca, CancellationToken cancellationToken = default)
        {
            await _context.Pecas.AddAsync(peca, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return peca;
        }

        public async Task UpdateAsync(Peca peca, CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var peca = await _context.Pecas.FindAsync(new object[] { id }, cancellationToken);
            if (peca != null)
            {
                _context.Pecas.Remove(peca);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Pecas.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken);
    }
}
