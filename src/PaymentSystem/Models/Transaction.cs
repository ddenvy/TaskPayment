using System;

namespace PaymentSystem.Models;

public enum TransactionStatus
{
    Pending,
    Processed,
    Failed,
    Refunded
}

public class Transaction
{
    public required string Id { get; set; }
    public required PaymentRequest Request { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? GatewayUsed { get; set; }
    public decimal Commission { get; set; }
    public string? ErrorMessage { get; set; }
}