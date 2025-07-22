using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class MockBalanceService : IBalanceService
{
    public bool HasSufficientBalance(string account, decimal amount, Currency currency)
    {
        // Mock logic: assume balance is always sufficient for demo
        return true;
    }
}