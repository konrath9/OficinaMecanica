using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public UsuarioRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        public async Task<Usuario> AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            await _context.Usuarios.AddAsync(usuario, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return usuario;
        }

        public async Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
            => await _context.Usuarios.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken);
    }
}
