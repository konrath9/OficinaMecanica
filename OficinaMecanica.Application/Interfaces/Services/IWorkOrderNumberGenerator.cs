namespace OficinaMecanica.Application.Interfaces.Services
{
    public interface IWorkOrderNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
