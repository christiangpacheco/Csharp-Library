namespace Library.Services;

// Seção "Smtp" da configuração. Em dev aponta para o Mailpit (docker-compose); para envio real,
// troque Host/Port/Username/Password nos User Secrets (ex.: smtp-relay.brevo.com:587).
public class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    // STARTTLS na porta 587. O Mailpit local não usa TLS, então em dev fica false.
    public bool UseStartTls { get; set; } = true;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Biblioteca";
}
