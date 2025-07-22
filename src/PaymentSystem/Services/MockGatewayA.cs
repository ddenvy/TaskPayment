using System;
using System.Threading.Tasks;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class MockGatewayA : IPaymentGateway
{
    public string Name => "GatewayA";

    public async Task<decimal> GetCommissionAsync(Currency currency)
    {
        return await Task.FromResult(currency == Currency.USD ? 0.01m : 0.02m);
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await Task.FromResult(true);
    }

    public bool SupportsCurrency(Currency currency)
    {
        return currency == Currency.USD || currency == Currency.EUR;
    }

    public async Task<bool> ProcessPaymentAsync(PaymentRequest request)
    {
        // Mock processing
        return await Task.FromResult(true);
    }

    public async Task<bool> RefundAsync(string transactionId, decimal amount)
    {
        // Mock refund
        return await Task.FromResult(true);
    }
}