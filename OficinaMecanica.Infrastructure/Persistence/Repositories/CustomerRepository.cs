using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Application.Interfaces.Repositories;
using OficinaMecanica.Domain.Entities;

namespace OficinaMecanica.Infrastructure.Persistence.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly OficinaMecanicaDbContext _context;

        public CustomerRepository(OficinaMecanicaDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Document == document, cancellationToken);
        }

        public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            await _context.Customers.AddAsync(customer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return customer;
        }

        public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var customer = await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.Id == id, cancellationToken);
        }
    }
}

