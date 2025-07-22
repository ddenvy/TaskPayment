using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class PaymentProcessor
{
    private readonly IPaymentValidator _validator;
    private readonly IPaymentRouter _router;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<PaymentProcessor> _logger;
    private readonly ConcurrentDictionary<string, Transaction> _transactionLog = new();

    public Transaction GetTransaction(string transactionId)
    {
        _transactionLog.TryGetValue(transactionId, out var transaction);
        return transaction;
    }
    private readonly AsyncRetryPolicy _retryPolicy;

    public PaymentProcessor(IPaymentValidator validator, IPaymentRouter router, IExchangeRateService exchangeRateService, ILogger<PaymentProcessor> logger)
    {
        _validator = validator;
        _router = router;
        _exchangeRateService = exchangeRateService;
        _logger = logger;

        _retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} after {TimeSpan} due to {Exception}", retryCount, timeSpan, exception.Message);
                });
    }

    public async Task<Transaction> ProcessPaymentAsync(PaymentRequest request, string transactionId, Currency? targetCurrency = null)
    {
        if (_transactionLog.TryGetValue(transactionId, out var existing) && existing.Status == TransactionStatus.Processed)
        {
            _logger.LogInformation("Idempotent: Transaction {TransactionId} already processed", transactionId);
            return existing;
        }

        var transaction = new Transaction { Id = transactionId, Request = request, Status = TransactionStatus.Pending, Timestamp = DateTime.UtcNow };
        _transactionLog[transactionId] = transaction;

        try
        {
            if (!_validator.Validate(request))
            {
                throw new InvalidOperationException("Validation failed");
            }

            if (targetCurrency.HasValue && targetCurrency != request.Currency)
            {
                var rate = await _exchangeRateService.GetExchangeRateAsync(request.Currency, targetCurrency.Value);
                request.Amount *= rate;
                request.Currency = targetCurrency.Value;
                _logger.LogInformation("Converted {OriginalAmount} {OriginalCurrency} to {NewAmount} {NewCurrency}", request.Amount / rate, request.Currency, request.Amount, targetCurrency);
            }

            var gateway = await _router.SelectOptimalGatewayAsync(request);
            transaction.GatewayUsed = gateway.Name;
            transaction.Commission = await gateway.GetCommissionAsync(request.Currency);

            var success = await _retryPolicy.ExecuteAsync(async () => await gateway.ProcessPaymentAsync(request));

            transaction.Status = success ? TransactionStatus.Processed : TransactionStatus.Failed;
        }
        catch (Exception ex)
        {
            transaction.Status = TransactionStatus.Failed;
            transaction.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Payment processing failed for {TransactionId}", transactionId);
        }

        return transaction;
    }

    public async Task<Transaction> RefundAsync(string transactionId, decimal amount)
    {
        if (!_transactionLog.TryGetValue(transactionId, out var transaction) || transaction.Status != TransactionStatus.Processed)
        {
            throw new InvalidOperationException("Cannot refund non-processed transaction");
        }

        var gateway = _router.GetGatewayByName(transaction.GatewayUsed);
        if (gateway == null)
        {
            throw new InvalidOperationException("Gateway not found for refund");
        }

        var success = await gateway.RefundAsync(transactionId, amount);
        if (success)
        {
            transaction.Status = TransactionStatus.Refunded;
            _logger.LogInformation("Refunded {Amount} for {TransactionId}", amount, transactionId);
        }

        return transaction;
    }

    // Simulate notification handling
    public void HandleNotification(string transactionId, string status)
    {
        if (_transactionLog.TryGetValue(transactionId, out var transaction))
        {
            transaction.Status = Enum.TryParse<TransactionStatus>(status, out var newStatus) ? newStatus : transaction.Status;
            _logger.LogInformation("Notification updated {TransactionId} to {Status}", transactionId, transaction.Status);
        }
    }
}