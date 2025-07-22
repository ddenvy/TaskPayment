using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentSystem.Models;
using PaymentSystem.Services;
using Xunit;

namespace PaymentSystem.Tests;

public class PaymentRouterTests
{
    private readonly Mock<ILogger<PaymentRouter>> _loggerMock;
    private readonly List<IPaymentGateway> _gateways;
    private readonly PaymentRouter _router;

    public PaymentRouterTests()
    {
        _loggerMock = new Mock<ILogger<PaymentRouter>>();
        _gateways = new List<IPaymentGateway>
        {
            new MockGatewayA(),
            new MockGatewayB()
        };
        _router = new PaymentRouter(_gateways, _loggerMock.Object);
    }

    [Fact]
    public async Task SelectOptimalGatewayAsync_SelectsLowestCommission()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.EUR, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };

        var gateway = await _router.SelectOptimalGatewayAsync(request);

        Assert.Equal("GatewayB", gateway.Name); // GatewayB has lower commission for EUR (0.015 vs 0.02)
    }

    [Fact]
    public async Task SelectOptimalGatewayAsync_ThrowsWhenNoAvailable()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = (Currency)999, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() }; // Unsupported

        await Assert.ThrowsAsync<System.InvalidOperationException>(() => _router.SelectOptimalGatewayAsync(request));
    }

    // Add more tests for availability, currency support, etc.
}