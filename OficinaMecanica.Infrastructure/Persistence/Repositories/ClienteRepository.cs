using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public ClienteRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task<Cliente?> GetByDocumentoAsync(string documento, CancellationToken cancellationToken = default)
            => await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Documento == documento, cancellationToken);

        public async Task<IEnumerable<Cliente>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Clientes.AsNoTracking().ToListAsync(cancellationToken);

        public async Task<Cliente> AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            await _context.Clientes.AddAsync(cliente, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return cliente;
        }

        public async Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cliente = await _context.Clientes.FindAsync(new object[] { id }, cancellationToken);
            if (cliente != null)
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Clientes.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);
    }
}

