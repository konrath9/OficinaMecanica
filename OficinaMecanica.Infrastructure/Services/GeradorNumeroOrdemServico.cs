using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Application.Interfaces.Services;

namespace OficinaMecanica.Infrastructure.Services
{
    public class GeradorNumeroOrdemServico : IGeradorNumeroOrdemServico
    {
        public Task<string> GerarAsync(CancellationToken cancellationToken = default)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var aleatorio = Random.Shared.Next(1000, 9999);
            var numero = $"OS-{timestamp}-{aleatorio}";

            return Task.FromResult(numero);
        }
    }
}
