using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class MockExchangeRateService : IExchangeRateService
{
    private readonly IMemoryCache _cache;

    public MockExchangeRateService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<decimal> GetExchangeRateAsync(Currency from, Currency to)
    {
        if (from == to) return 1m;

        var key = $"{from}-{to}";
        if (_cache.TryGetValue(key, out decimal rate))
        {
            return rate;
        }

        // Mock API call
        rate = from switch
        {
            Currency.USD when to == Currency.EUR => 0.85m,
            Currency.USD when to == Currency.RUB => 90m,
            Currency.EUR when to == Currency.USD => 1.18m,
            Currency.EUR when to == Currency.RUB => 100m,
            Currency.RUB when to == Currency.USD => 0.011m,
            Currency.RUB when to == Currency.EUR => 0.01m,
            _ => throw new NotSupportedException($"Conversion from {from} to {to} not supported")
        };

        _cache.Set(key, rate, TimeSpan.FromMinutes(5)); // Cache for 5 minutes
        return await Task.FromResult(rate);
    }
}