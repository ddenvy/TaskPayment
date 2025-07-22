using PaymentSystem.Models;

namespace PaymentSystem.Services;

public interface IPaymentValidator
{
    bool Validate(PaymentRequest request);
}