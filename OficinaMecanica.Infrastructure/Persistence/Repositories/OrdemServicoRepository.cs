using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class OrdemServicoRepository : IOrdemServicoRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public OrdemServicoRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.OrdensServico
                .Include(w => w.Servicos)
                .Include(w => w.Pecas)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }

        public async Task<OrdemServico?> GetByNumeroAsync(string numero, CancellationToken cancellationToken = default)
        {
            return await _context.OrdensServico
                .AsNoTracking()
                .Include(w => w.Servicos)
                .Include(w => w.Pecas)
                .FirstOrDefaultAsync(w => w.Numero == numero, cancellationToken);
        }

        public async Task<IEnumerable<OrdemServico>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.OrdensServico
                .AsNoTracking()
                .Include(w => w.Servicos)
                .Include(w => w.Pecas)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrdemServico> AddAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
        {
            await _context.OrdensServico.AddAsync(ordemServico, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return ordemServico;
        }

        public async Task UpdateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.OrdensServico
                .AsNoTracking()
                .AnyAsync(w => w.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<OrdemServico>> GetFinalizadasNoPeriodoAsync(
            DateTime? inicio,
            DateTime? fim,
            CancellationToken cancellationToken = default)
        {
            var query = _context.OrdensServico
                .AsNoTracking()
                .Where(os => os.Status == StatusOrdemServico.Finalizada || os.Status == StatusOrdemServico.Entregue);

            if (inicio.HasValue)
                query = query.Where(os => os.FinalizadaEm >= inicio.Value);

            if (fim.HasValue)
                query = query.Where(os => os.FinalizadaEm <= fim.Value);

            return await query.ToListAsync(cancellationToken);
        }
    }
}
