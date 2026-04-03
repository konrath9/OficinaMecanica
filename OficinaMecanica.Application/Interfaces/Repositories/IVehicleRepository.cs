namespace OficinaMecanica.Application.Interfaces.Repositories
{
    public interface IVehicleRepository
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
