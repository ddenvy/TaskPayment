using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PaymentSystem.Interfaces;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class ImprovedMockGateway : IPaymentGatewayV2
{
    private readonly ConcurrentDictionary<string, PaymentResult> _processedPayments = new();
    private readonly ConcurrentDictionary<string, RefundResult> _processedRefunds = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _paymentSemaphores = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refundSemaphores = new();
    private readonly string _gatewayName;
    private readonly decimal _commissionRate;
    private readonly Currency[] _supportedCurrencies;
    private readonly Random _random = new();

    public ImprovedMockGateway(string gatewayName, decimal commissionRate, params Currency[] supportedCurrencies)
    {
        _gatewayName = gatewayName;
        _commissionRate = commissionRate;
        _supportedCurrencies = supportedCurrencies;
    }

    public string Name => _gatewayName;

    public async Task<decimal> GetCommissionAsync(Currency currency)
    {
        await Task.Delay(10); // Симуляция сетевого запроса
        return _commissionRate;
    }

    public async Task<bool> IsAvailableAsync()
    {
        await Task.Delay(5); // Симуляция проверки доступности
        // Симулируем 95% доступности
        return _random.NextDouble() > 0.05;
    }

    public bool SupportsCurrency(Currency currency)
    {
        return Array.Exists(_supportedCurrencies, c => c == currency);
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, string transactionId)
    {
        // Проверка идемпотентности - если уже обработано, возвращаем результат
        if (_processedPayments.TryGetValue(transactionId, out var existingResult))
        {
            return existingResult;
        }

        // Получаем семафор для данной транзакции (атомарно)
        var semaphore = _paymentSemaphores.GetOrAdd(transactionId, _ => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync();
        try
        {
            // Повторная проверка после получения блокировки
            if (_processedPayments.TryGetValue(transactionId, out var reCheckResult))
            {
                return reCheckResult;
            }

            // Валидация запроса
            if (!SupportsCurrency(request.Currency))
            {
                var errorResult = new PaymentResult
                {
                    IsSuccess = false,
                    Status = PaymentStatus.Failed,
                    ErrorCode = "UNSUPPORTED_CURRENCY",
                    ErrorMessage = $"Currency {request.Currency} is not supported by {Name}",
                    ProcessedAt = DateTime.UtcNow,
                    IsRetryable = false
                };
                
                _processedPayments.TryAdd(transactionId, errorResult);
                return errorResult;
            }

            // Симуляция обработки платежа
            await Task.Delay(_random.Next(100, 500)); // Реалистичная задержка

            // Симуляция различных исходов
            var outcome = _random.NextDouble();
            PaymentResult result;

            if (outcome < 0.85) // 85% успешных платежей
            {
                result = new PaymentResult
                {
                    IsSuccess = true,
                    GatewayTransactionId = $"{Name.ToLower()}_{Guid.NewGuid():N}"[..16],
                    Status = PaymentStatus.Completed,
                    ProcessedAt = DateTime.UtcNow,
                    IsRetryable = false,
                    ActualAmount = request.Amount - (request.Amount * _commissionRate),
                    ProviderReference = $"REF_{DateTime.UtcNow:yyyyMMddHHmmss}_{_random.Next(1000, 9999)}"
                };
            }
            else if (outcome < 0.95) // 10% временных ошибок
            {
                result = new PaymentResult
                {
                    IsSuccess = false,
                    Status = PaymentStatus.Failed,
                    ErrorCode = "TEMPORARY_ERROR",
                    ErrorMessage = "Temporary network error, please retry",
                    ProcessedAt = DateTime.UtcNow,
                    IsRetryable = true
                };
            }
            else // 5% постоянных ошибок
            {
                result = new PaymentResult
                {
                    IsSuccess = false,
                    Status = PaymentStatus.Failed,
                    ErrorCode = "INSUFFICIENT_FUNDS",
                    ErrorMessage = "Insufficient funds in source account",
                    ProcessedAt = DateTime.UtcNow,
                    IsRetryable = false
                };
            }

            // Атомарное сохранение результата
            _processedPayments.TryAdd(transactionId, result);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<PaymentResult> GetPaymentStatusAsync(string transactionId)
    {
        await Task.Delay(10); // Симуляция запроса к API
        
        if (_processedPayments.TryGetValue(transactionId, out var result))
        {
            return result;
        }

        // Транзакция не найдена
        return new PaymentResult
        {
            IsSuccess = false,
            Status = PaymentStatus.Failed,
            ErrorCode = "TRANSACTION_NOT_FOUND",
            ErrorMessage = $"Transaction {transactionId} not found in {Name}",
            ProcessedAt = DateTime.UtcNow,
            IsRetryable = false
        };
    }

    public async Task<RefundResult> RefundAsync(string transactionId, decimal amount, string refundId)
    {
        // Проверка идемпотентности для возвратов
        if (_processedRefunds.TryGetValue(refundId, out var existingRefund))
        {
            return existingRefund;
        }

        // Получаем семафор для данного возврата (атомарно)
        var semaphore = _refundSemaphores.GetOrAdd(refundId, _ => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync();
        try
        {
            // Повторная проверка после получения блокировки
            if (_processedRefunds.TryGetValue(refundId, out var reCheckResult))
            {
                return reCheckResult;
            }

            // Проверяем, существует ли оригинальная транзакция
            if (!_processedPayments.TryGetValue(transactionId, out var originalPayment) || 
                !originalPayment.IsSuccess)
            {
                var errorResult = new RefundResult
                {
                    IsSuccess = false,
                    Status = RefundStatus.Failed,
                    ErrorCode = "ORIGINAL_TRANSACTION_NOT_FOUND",
                    ErrorMessage = "Original transaction not found or was not successful",
                    ProcessedAt = DateTime.UtcNow,
                    OriginalTransactionId = transactionId
                };
                
                _processedRefunds.TryAdd(refundId, errorResult);
                return errorResult;
            }

            await Task.Delay(_random.Next(50, 200)); // Симуляция обработки возврата

            var refundResult = new RefundResult
            {
                IsSuccess = true,
                GatewayRefundId = $"{Name.ToLower()}_ref_{Guid.NewGuid():N}"[..20],
                Status = RefundStatus.Completed,
                ProcessedAt = DateTime.UtcNow,
                RefundedAmount = amount,
                OriginalTransactionId = transactionId
            };

            _processedRefunds.TryAdd(refundId, refundResult);
            return refundResult;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<RefundResult> GetRefundStatusAsync(string refundId)
    {
        await Task.Delay(10);
        
        if (_processedRefunds.TryGetValue(refundId, out var result))
        {
            return result;
        }

        return new RefundResult
        {
            IsSuccess = false,
            Status = RefundStatus.Failed,
            ErrorCode = "REFUND_NOT_FOUND",
            ErrorMessage = $"Refund {refundId} not found in {Name}",
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<PaymentResult> CancelPaymentAsync(string transactionId)
    {
        await Task.Delay(50);
        
        if (_processedPayments.TryGetValue(transactionId, out var payment))
        {
            if (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Processing)
            {
                // Обновляем статус на отменённый
                var cancelledResult = new PaymentResult
                {
                    IsSuccess = true,
                    GatewayTransactionId = payment.GatewayTransactionId,
                    Status = PaymentStatus.Cancelled,
                    ProcessedAt = DateTime.UtcNow,
                    IsRetryable = false
                };
                
                _processedPayments.TryUpdate(transactionId, cancelledResult, payment);
                return cancelledResult;
            }
        }

        return new PaymentResult
        {
            IsSuccess = false,
            Status = PaymentStatus.Failed,
            ErrorCode = "CANNOT_CANCEL",
            ErrorMessage = "Transaction cannot be cancelled",
            ProcessedAt = DateTime.UtcNow,
            IsRetryable = false
        };
    }
}