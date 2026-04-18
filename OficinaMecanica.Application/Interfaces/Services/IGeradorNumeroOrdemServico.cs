namespace OficinaMecanica.Application.Interfaces.Services
{
    public interface IGeradorNumeroOrdemServico
    {
        Task<string> GerarAsync(CancellationToken cancellationToken = default);
    }
}
