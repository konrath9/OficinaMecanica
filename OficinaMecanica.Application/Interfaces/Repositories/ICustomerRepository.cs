namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
