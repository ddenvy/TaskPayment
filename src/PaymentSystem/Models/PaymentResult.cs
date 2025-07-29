using System;

namespace PaymentSystem.Models;

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public bool IsRetryable { get; set; }
    public decimal? ActualAmount { get; set; } // Фактическая сумма после комиссий
    public string ProviderReference { get; set; } = string.Empty; // Ссылка провайдера
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    RequiresAction, // для 3DS и подобных случаев
    PartiallyCompleted
}

public class RefundResult
{
    public bool IsSuccess { get; set; }
    public string GatewayRefundId { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public string OriginalTransactionId { get; set; } = string.Empty;
}

public enum RefundStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    PartiallyRefunded
}