using System;
using System.Threading.Tasks;
using PaymentSystem.Interfaces;
using PaymentSystem.Models;
using PaymentSystem.Services;

namespace PaymentSystem.Services;

/// <summary>
/// Адаптер для обеспечения совместимости между IPaymentGateway и IPaymentGatewayV2
/// </summary>
public class PaymentGatewayAdapter : IPaymentGatewayV2
{
    private readonly IPaymentGateway _legacyGateway;

    public PaymentGatewayAdapter(IPaymentGateway legacyGateway)
    {
        _legacyGateway = legacyGateway ?? throw new ArgumentNullException(nameof(legacyGateway));
    }

    public string Name => _legacyGateway.Name;

    public Task<decimal> GetCommissionAsync(Currency currency) => _legacyGateway.GetCommissionAsync(currency);

    public Task<bool> IsAvailableAsync() => _legacyGateway.IsAvailableAsync();

    public bool SupportsCurrency(Currency currency) => _legacyGateway.SupportsCurrency(currency);

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, string transactionId)
    {
        try
        {
            var success = await _legacyGateway.ProcessPaymentAsync(request);
            
            return new PaymentResult
            {
                IsSuccess = success,
                GatewayTransactionId = success ? $"{Name}_{transactionId}" : string.Empty,
                Status = success ? PaymentStatus.Completed : PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                IsRetryable = !success, // Предполагаем, что неудачи можно повторить
                ErrorCode = success ? string.Empty : "LEGACY_GATEWAY_ERROR",
                ErrorMessage = success ? string.Empty : "Legacy gateway returned false"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                IsRetryable = true, // Исключения обычно можно повторить
                ErrorCode = "LEGACY_GATEWAY_EXCEPTION",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> GetPaymentStatusAsync(string transactionId)
    {
        // Старый интерфейс не поддерживает проверку статуса
        // Возвращаем "не найдено"
        await Task.CompletedTask;
        
        return new PaymentResult
        {
            IsSuccess = false,
            Status = PaymentStatus.Failed,
            ErrorCode = "NOT_SUPPORTED",
            ErrorMessage = "Legacy gateway does not support status checking",
            ProcessedAt = DateTime.UtcNow,
            IsRetryable = false
        };
    }

    public async Task<RefundResult> RefundAsync(string transactionId, decimal amount, string refundId)
    {
        try
        {
            var success = await _legacyGateway.RefundAsync(transactionId, amount);
            
            return new RefundResult
            {
                IsSuccess = success,
                GatewayRefundId = success ? $"{Name}_ref_{refundId}" : string.Empty,
                Status = success ? RefundStatus.Completed : RefundStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                RefundedAmount = success ? amount : 0,
                OriginalTransactionId = transactionId,
                ErrorCode = success ? string.Empty : "LEGACY_REFUND_ERROR",
                ErrorMessage = success ? string.Empty : "Legacy gateway refund returned false"
            };
        }
        catch (Exception ex)
        {
            return new RefundResult
            {
                IsSuccess = false,
                Status = RefundStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                RefundedAmount = 0,
                OriginalTransactionId = transactionId,
                ErrorCode = "LEGACY_REFUND_EXCEPTION",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<RefundResult> GetRefundStatusAsync(string refundId)
    {
        // Старый интерфейс не поддерживает проверку статуса возврата
        await Task.CompletedTask;
        
        return new RefundResult
        {
            IsSuccess = false,
            Status = RefundStatus.Failed,
            ErrorCode = "NOT_SUPPORTED",
            ErrorMessage = "Legacy gateway does not support refund status checking",
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<PaymentResult> CancelPaymentAsync(string transactionId)
    {
        // Старый интерфейс не поддерживает отмену платежей
        await Task.CompletedTask;
        
        return new PaymentResult
        {
            IsSuccess = false,
            Status = PaymentStatus.Failed,
            ErrorCode = "NOT_SUPPORTED",
            ErrorMessage = "Legacy gateway does not support payment cancellation",
            ProcessedAt = DateTime.UtcNow,
            IsRetryable = false
        };
    }
}

/// <summary>
/// Обратный адаптер для использования IPaymentGatewayV2 как IPaymentGateway
/// </summary>
public class PaymentGatewayReverseAdapter : IPaymentGateway
{
    private readonly IPaymentGatewayV2 _modernGateway;

    public PaymentGatewayReverseAdapter(IPaymentGatewayV2 modernGateway)
    {
        _modernGateway = modernGateway ?? throw new ArgumentNullException(nameof(modernGateway));
    }

    public string Name => _modernGateway.Name;

    public Task<decimal> GetCommissionAsync(Currency currency) => _modernGateway.GetCommissionAsync(currency);

    public Task<bool> IsAvailableAsync() => _modernGateway.IsAvailableAsync();

    public bool SupportsCurrency(Currency currency) => _modernGateway.SupportsCurrency(currency);

    public async Task<bool> ProcessPaymentAsync(PaymentRequest request)
    {
        // Генерируем временный ID для совместимости
        var tempTransactionId = Guid.NewGuid().ToString();
        var result = await _modernGateway.ProcessPaymentAsync(request, tempTransactionId);
        return result.IsSuccess;
    }

    public async Task<bool> RefundAsync(string transactionId, decimal amount)
    {
        // Генерируем временный ID возврата
        var tempRefundId = Guid.NewGuid().ToString();
        var result = await _modernGateway.RefundAsync(transactionId, amount, tempRefundId);
        return result.IsSuccess;
    }
}