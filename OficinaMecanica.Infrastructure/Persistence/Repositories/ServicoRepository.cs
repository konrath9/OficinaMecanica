using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class ServicoRepository : IServicoRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public ServicoRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Servico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Servicos.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        public async Task<Servico?> GetByNomeAsync(string nome, CancellationToken cancellationToken = default)
            => await _context.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Nome == nome, cancellationToken);

        public async Task<IEnumerable<Servico>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Servicos.AsNoTracking().ToListAsync(cancellationToken);

        public async Task<Servico> AddAsync(Servico servico, CancellationToken cancellationToken = default)
        {
            await _context.Servicos.AddAsync(servico, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return servico;
        }

        public async Task UpdateAsync(Servico servico, CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var servico = await _context.Servicos.FindAsync(new object[] { id }, cancellationToken);
            if (servico != null)
            {
                _context.Servicos.Remove(servico);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Servicos.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);
    }
}
