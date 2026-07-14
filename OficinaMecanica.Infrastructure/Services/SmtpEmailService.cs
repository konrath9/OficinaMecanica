using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using OficinaMecanica.Application.DTOs.OrdemServico;
using OficinaMecanica.Application.Interfaces.Services;
using OficinaMecanica.Infrastructure.Settings;

namespace OficinaMecanica.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public SmtpEmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task EnviarNotificacaoStatusAsync(NotificacaoStatusOrdemServico notificacao, CancellationToken cancellationToken = default)
        {
            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(_settings.RemetenteNome, _settings.RemetenteEmail));
            mensagem.To.Add(new MailboxAddress(notificacao.DestinatarioNome, notificacao.DestinatarioEmail));
            mensagem.Subject = $"OS {notificacao.NumeroOrdemServico} - {notificacao.StatusAtual}";

            var corpo = $"Olá, {notificacao.DestinatarioNome}!\n\n" +
                        $"A sua Ordem de Serviço {notificacao.NumeroOrdemServico} teve o status atualizado para: {notificacao.StatusAtual}.\n" +
                        (string.IsNullOrWhiteSpace(notificacao.Observacao) ? string.Empty : $"\nObservação: {notificacao.Observacao}\n") +
                        "\nAtenciosamente,\nOficina Mecânica";

            mensagem.Body = new TextPart("plain") { Text = corpo };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.None, cancellationToken);
            await client.SendAsync(mensagem, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
