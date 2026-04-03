using OficinaMecanica.Application.Interfaces.Services;

namespace OficinaMecanica.Infrastructure.Services
{
    public class WorkOrderNumberGenerator : IWorkOrderNumberGenerator
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = Random.Shared.Next(1000, 9999);
            var number = $"OS-{timestamp}-{random}";
            
            return Task.FromResult(number);
        }
    }
}
