using Library.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Library.Services;

// Implementação de desenvolvimento: não manda e-mail de verdade, apenas registra o link no log,
// para dar para testar o fluxo de reset sem um provedor externo configurado. A implementação de
// produção (serviço externo com EmailApiKey) entra como evolução, junto do Key Vault na Leva 8.
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
