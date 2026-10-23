namespace Library.Services.Interfaces;

// Abstração de envio de e-mail. Em produção, uma implementação usa um serviço externo
// (chave em EmailApiKey nos User Secrets / Key Vault); em desenvolvimento, o link é só registrado no log.
public interface IEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string resetLink);
}
