using System.Threading.Tasks;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public interface IPaymentGateway
{
    string Name { get; }
    Task<decimal> GetCommissionAsync(Currency currency);
    Task<bool> IsAvailableAsync();
    bool SupportsCurrency(Currency currency);
    Task<bool> ProcessPaymentAsync(PaymentRequest request);
    Task<bool> RefundAsync(string transactionId, decimal amount);
}