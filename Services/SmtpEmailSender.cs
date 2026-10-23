using System.Net;
using Library.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Library.Services;

// Envia e-mail por SMTP (MailKit). Funciona com o Mailpit em dev e com qualquer provedor SMTP
// (Brevo, Gmail com senha de app) só trocando a configuração "Smtp".
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Redefinição de senha - Biblioteca";

        var encodedLink = WebUtility.HtmlEncode(resetLink);
        message.Body = new BodyBuilder
        {
            TextBody = $"Recebemos um pedido para redefinir a sua senha.\n\nAbra o link abaixo para criar uma nova senha:\n{resetLink}\n\nSe não foi você, ignore este e-mail.",
            HtmlBody = $"<p>Recebemos um pedido para redefinir a sua senha.</p><p><a href=\"{encodedLink}\">Criar uma nova senha</a></p><p>Se não foi você, ignore este e-mail.</p>"
        }.ToMessageBody();

        using var client = new SmtpClient();
        var security = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_options.Host!, _options.Port, security);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
