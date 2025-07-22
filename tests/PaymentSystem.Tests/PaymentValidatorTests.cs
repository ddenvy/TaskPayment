using Moq;
using Microsoft.Extensions.Logging;
using PaymentSystem.Models;
using PaymentSystem.Services;
using Xunit;

namespace PaymentSystem.Tests;

public class PaymentValidatorTests
{
    private readonly Mock<ILogger<PaymentValidator>> _loggerMock;
    private readonly Mock<IBalanceService> _balanceServiceMock;
    private readonly PaymentValidator _validator;

    public PaymentValidatorTests()
    {
        _loggerMock = new Mock<ILogger<PaymentValidator>>();
        _balanceServiceMock = new Mock<IBalanceService>();
        _validator = new PaymentValidator(_loggerMock.Object, _balanceServiceMock.Object);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsTrue()
    {
        var request = new PaymentRequest
        {
            Amount = 100m,
            Currency = Currency.USD,
            SourceAccount = "1234567890",
            DestinationAccount = "0987654321",
            Metadata = new System.Collections.Generic.Dictionary<string, string>()
        };
        _balanceServiceMock.Setup(x => x.HasSufficientBalance(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Currency>())).Returns(true);

        var result = _validator.Validate(request);

        Assert.True(result);
    }

    [Fact]
    public void Validate_InvalidAccountFormat_ReturnsFalse()
    {
        var request = new PaymentRequest
        {
            Amount = 100m,
            Currency = Currency.USD,
            SourceAccount = "invalid",
            DestinationAccount = "0987654321",
            Metadata = new System.Collections.Generic.Dictionary<string, string>()
        };
        _balanceServiceMock.Setup(x => x.HasSufficientBalance(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Currency>())).Returns(true);

        var result = _validator.Validate(request);

        Assert.False(result);
    }

    [Fact]
    public void Validate_ExceedsLimit_ReturnsFalse()
    {
        var request = new PaymentRequest
        {
            Amount = 20000m,
            Currency = Currency.USD,
            SourceAccount = "1234567890",
            DestinationAccount = "0987654321",
            Metadata = new System.Collections.Generic.Dictionary<string, string>()
        };
        _balanceServiceMock.Setup(x => x.HasSufficientBalance(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Currency>())).Returns(true);

        var result = _validator.Validate(request);

        Assert.False(result);
    }

    [Fact]
    public void Validate_InsufficientBalance_ReturnsFalse()
    {
        var request = new PaymentRequest
        {
            Amount = 100m,
            Currency = Currency.USD,
            SourceAccount = "1234567890",
            DestinationAccount = "0987654321",
            Metadata = new System.Collections.Generic.Dictionary<string, string>()
        };
        _balanceServiceMock.Setup(x => x.HasSufficientBalance(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Currency>())).Returns(false);

        var result = _validator.Validate(request);

        Assert.False(result);
    }

    // Добавьте тесты на граничные случаи, например, Amount = 0, Amount = max limit, invalid currency etc.
    [Fact]
    public void Validate_ZeroAmount_ReturnsFalse()
    {
        var request = new PaymentRequest
        {
            Amount = 0m,
            Currency = Currency.USD,
            SourceAccount = "1234567890",
            DestinationAccount = "0987654321",
            Metadata = new System.Collections.Generic.Dictionary<string, string>()
        };
        _balanceServiceMock.Setup(x => x.HasSufficientBalance(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<Currency>())).Returns(true);

        var result = _validator.Validate(request);

        Assert.False(result);
    }
}