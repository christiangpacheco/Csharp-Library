namespace Library.Services;

// Parâmetros do reset de senha (seção "PasswordReset" do appsettings.json).
public class PasswordResetOptions
{
    // Por quanto tempo o link de redefinição vale, em minutos.
    public int TokenLifetimeMinutes { get; set; } = 60;
}
