using PaymentSystem.Models;

namespace PaymentSystem.Services;

public interface IBalanceService
{
    bool HasSufficientBalance(string account, decimal amount, Currency currency);
}