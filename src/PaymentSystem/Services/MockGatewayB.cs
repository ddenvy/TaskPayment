using System;
using System.Threading.Tasks;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class MockGatewayB : IPaymentGateway
{
    public string Name => "GatewayB";

    public async Task<decimal> GetCommissionAsync(Currency currency)
    {
        return await Task.FromResult(currency == Currency.EUR ? 0.015m : 0.025m);
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await Task.FromResult(true);
    }

    public bool SupportsCurrency(Currency currency)
    {
        return currency == Currency.EUR || currency == Currency.RUB;
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