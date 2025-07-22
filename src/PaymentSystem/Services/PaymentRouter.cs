using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PaymentSystem.Models;

namespace PaymentSystem.Services;

public class PaymentRouter : IPaymentRouter
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly ILogger<PaymentRouter> _logger;

    public PaymentRouter(IEnumerable<IPaymentGateway> gateways, ILogger<PaymentRouter> logger)
    {
        _gateways = gateways;
        _logger = logger;
    }

    public IPaymentGateway GetGatewayByName(string name)
    {
        return _gateways.FirstOrDefault(g => g.Name == name);
    }

    public async Task<IPaymentGateway> SelectOptimalGatewayAsync(PaymentRequest request)
    {
        var availableGateways = new List<(IPaymentGateway Gateway, decimal Commission)>();

        foreach (var gateway in _gateways)
        {
            if (gateway.SupportsCurrency(request.Currency) && await gateway.IsAvailableAsync())
            {
                var commission = await gateway.GetCommissionAsync(request.Currency);
                availableGateways.Add((gateway, commission));
            }
        }

        if (!availableGateways.Any())
        {
            _logger.LogError("No available gateways for {Currency}", request.Currency);
            throw new InvalidOperationException("No available payment gateways");
        }

        var optimal = availableGateways.OrderBy(g => g.Commission).First().Gateway;
        _logger.LogInformation("Selected {GatewayName} for {Currency} with commission {Commission}", optimal.Name, request.Currency, availableGateways.First(g => g.Gateway == optimal).Commission);
        return optimal;
    }
}