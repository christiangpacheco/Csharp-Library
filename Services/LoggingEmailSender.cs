using Library.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Library.Services;

// Fallback sem SMTP configurado: não manda e-mail de verdade, apenas registra o link no log.
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        _logger.LogInformation("E-mail de redefinição de senha para {Email}. Link: {ResetLink}", toEmail, resetLink);
        return Task.CompletedTask;
    }
}
