namespace Library.Services;

// Limites de empréstimo configuráveis (seção "LoanRules" do appsettings.json).
public class LoanRules
{
    public int MaxOpenLoansPerUser { get; set; } = 3;

    public int LoanPeriodDays { get; set; } = 14;
}
