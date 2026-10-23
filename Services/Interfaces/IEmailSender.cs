namespace Library.Services.Interfaces;

// Abstração de envio de e-mail: SmtpEmailSender (Mailpit em dev, Brevo/Gmail em produção)
// ou LoggingEmailSender quando a seção "Smtp" não está configurada.
public interface IEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string resetLink);
}
