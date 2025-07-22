using System;
using System.Text.RegularExpressions;
using PaymentSystem.Models;
using Microsoft.Extensions.Logging;

namespace PaymentSystem.Services;

public class PaymentValidator : IPaymentValidator
{
    private readonly ILogger<PaymentValidator> _logger;
    private readonly IBalanceService _balanceService; // Assume this interface exists

    public PaymentValidator(ILogger<PaymentValidator> logger, IBalanceService balanceService)
    {
        _logger = logger;
        _balanceService = balanceService;
    }

    public virtual bool Validate(PaymentRequest request)
    {
        if (!ValidateAccountFormat(request.SourceAccount, request.Currency))
        {
            _logger.LogWarning("Invalid source account format for {Currency}", request.Currency);
            return false;
        }

        if (!ValidateAccountFormat(request.DestinationAccount, request.Currency))
        {
            _logger.LogWarning("Invalid destination account format for {Currency}", request.Currency);
            return false;
        }

        if (!ValidateLimits(request.Amount, request.Currency))
        {
            _logger.LogWarning("Amount exceeds limit for {Currency}", request.Currency);
            return false;
        }

        if (!_balanceService.HasSufficientBalance(request.SourceAccount, request.Amount, request.Currency))
        {
            _logger.LogWarning("Insufficient balance for {SourceAccount}", request.SourceAccount);
            return false;
        }

        return true;
    }

    private bool ValidateAccountFormat(string account, Currency currency)
    {
        // Example regex patterns
        string pattern = currency switch
        {
            Currency.USD => "^[0-9]{10}$", // Example
            Currency.EUR => "^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,32}$", // IBAN-like
            Currency.RUB => "^[0-9]{20}$", // Example
            _ => "^.*$"
        };
        return Regex.IsMatch(account, pattern);
    }

    private bool ValidateLimits(decimal amount, Currency currency)
    {
        decimal maxLimit = currency switch
        {
            Currency.USD => 10000m,
            Currency.EUR => 8000m,
            Currency.RUB => 500000m,
            _ => 0m
        };
        return amount > 0 && amount <= maxLimit;
    }
}