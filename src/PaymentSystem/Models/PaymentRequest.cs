using System.Collections.Generic;

namespace PaymentSystem.Models;

public class PaymentRequest
{
    public required decimal Amount { get; set; }
    public required Currency Currency { get; set; }
    public required string SourceAccount { get; set; }
    public required string DestinationAccount { get; set; }
    public required Dictionary<string, string> Metadata { get; set; }
}