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

    // Checagem de revogação (CRL/OCSP) do certificado do servidor. Fica desligada por padrão porque
    // intermediários do Let's Encrypt (usados pelo Brevo) não publicam mais CRL, e a conexão falharia
    // mesmo com a cadeia válida. Nome do host, validade e cadeia continuam sendo verificados.
    public bool CheckCertificateRevocation { get; set; }

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Biblioteca";
}
