using OficinaMecanica.Application.DTOs.OrdemServico;

namespace OficinaMecanica.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task EnviarNotificacaoStatusAsync(NotificacaoStatusOrdemServico notificacao, CancellationToken cancellationToken = default);
    }
}
