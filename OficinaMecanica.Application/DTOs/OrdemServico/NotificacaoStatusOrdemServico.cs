using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.DTOs.OrdemServico
{
    public record NotificacaoStatusOrdemServico(
        string DestinatarioEmail,
        string DestinatarioNome,
        string NumeroOrdemServico,
        StatusOrdemServico StatusAtual,
        string? Observacao = null);
}
