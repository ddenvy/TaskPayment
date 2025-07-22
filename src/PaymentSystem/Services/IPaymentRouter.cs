using System.Threading.Tasks;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public interface IPaymentRouter
{
    IPaymentGateway GetGatewayByName(string name);
    Task<IPaymentGateway> SelectOptimalGatewayAsync(PaymentRequest request);
}