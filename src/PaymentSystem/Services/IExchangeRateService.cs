using System.Threading.Tasks;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public interface IExchangeRateService
{
    Task<decimal> GetExchangeRateAsync(Currency from, Currency to);
}