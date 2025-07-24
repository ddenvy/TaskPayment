using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentSystem.Models;
using PaymentSystem.Services;
using Xunit;

namespace PaymentSystem.Tests;

public class PaymentProcessorTests
{
    private readonly Mock<IPaymentValidator> _validatorMock;
    private readonly Mock<IPaymentRouter> _routerMock;
    private readonly Mock<IExchangeRateService> _exchangeRateMock;
    private readonly Mock<ILogger<PaymentProcessor>> _loggerMock;
    private readonly Mock<IPaymentGateway> _gatewayMock;
    private readonly PaymentProcessor _processor;

    public PaymentProcessorTests()
    {
        _validatorMock = new Mock<IPaymentValidator>();
        _gatewayMock = new Mock<IPaymentGateway>();
        _routerMock = new Mock<IPaymentRouter>();
        _exchangeRateMock = new Mock<IExchangeRateService>();
        _loggerMock = new Mock<ILogger<PaymentProcessor>>();
        _processor = new PaymentProcessor(_validatorMock.Object, _routerMock.Object, _exchangeRateMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_ProcessesSuccessfully()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "test-id";
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(request)).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.ProcessPaymentAsync(request)).ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.USD)).ReturnsAsync(0.01m);

        var result = await _processor.ProcessPaymentAsync(request, transactionId);

        Assert.Equal(TransactionStatus.Processed, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Idempotent_ReturnsExisting()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "test-id";
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(request)).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.ProcessPaymentAsync(request)).ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.USD)).ReturnsAsync(0.01m);

        await _processor.ProcessPaymentAsync(request, transactionId);
        var result = await _processor.ProcessPaymentAsync(request, transactionId);

        Assert.Equal(TransactionStatus.Processed, result.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithConversion_AdjustsAmount()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "test-id";
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _exchangeRateMock.Setup(e => e.GetExchangeRateAsync(Currency.USD, Currency.EUR)).ReturnsAsync(0.85m);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(It.Is<PaymentRequest>(req => req.Currency == Currency.EUR))).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.ProcessPaymentAsync(It.Is<PaymentRequest>(req => req.Amount == 85m && req.Currency == Currency.EUR))).ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.EUR)).ReturnsAsync(0.01m);

        var result = await _processor.ProcessPaymentAsync(request, transactionId, Currency.EUR);

        Assert.Equal(TransactionStatus.Processed, result.Status);
        Assert.Equal(85m, request.Amount);
        Assert.Equal(Currency.EUR, request.Currency);
    }

    [Fact]
    public async Task RefundAsync_ValidTransaction_RefundsSuccessfully()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "test-id";
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(request)).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.Name).Returns("MockGateway");
_routerMock.Setup(r => r.GetGatewayByName("MockGateway")).Returns(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.ProcessPaymentAsync(request)).ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.USD)).ReturnsAsync(0.01m);
        _gatewayMock.Setup(g => g.RefundAsync(transactionId, 50m)).ReturnsAsync(true);

        await _processor.ProcessPaymentAsync(request, transactionId);
        var result = await _processor.RefundAsync(transactionId, 50m);

        Assert.Equal(TransactionStatus.Refunded, result.Status);
    }

    [Fact]
    public async Task HandleNotification_UpdatesStatus()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "test-id";
        // Assume processed first
        var transaction = new Transaction { Id = transactionId, Request = request, Status = TransactionStatus.Processed };
        // Use reflection or adjust to add to private dict if needed, but for test, perhaps expose or mock
        // For simplicity, process first
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(request)).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.ProcessPaymentAsync(request)).ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.USD)).ReturnsAsync(0.01m);
        await _processor.ProcessPaymentAsync(request, transactionId);

        _processor.HandleNotification(transactionId, "Failed");

        var updatedTransaction = _processor.GetTransaction(transactionId);
        Assert.Equal(TransactionStatus.Failed, updatedTransaction.Status);
    }

    // Add tests for retry: setup gateway to fail first few times
    [Fact]
    public async Task ProcessPaymentAsync_RetriesOnFailure()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "test-id";
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(request)).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.SetupSequence(g => g.ProcessPaymentAsync(request))
            .ThrowsAsync(new Exception("Fail1"))
            .ThrowsAsync(new Exception("Fail2"))
            .ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.USD)).ReturnsAsync(0.01m);

        var result = await _processor.ProcessPaymentAsync(request, transactionId);

        Assert.Equal(TransactionStatus.Processed, result.Status);
        _gatewayMock.Verify(g => g.ProcessPaymentAsync(request), Times.Exactly(3));
    }

    // Тест на идемпотентность в условиях конкурентного доступа
    [Fact]
    public async Task ProcessPaymentAsync_ConcurrentRequests_EnsuresIdempotency()
    {
        var request = new PaymentRequest { Amount = 100m, Currency = Currency.USD, SourceAccount = "1234567890", DestinationAccount = "0987654321", Metadata = new System.Collections.Generic.Dictionary<string, string>() };
        string transactionId = "concurrent-test-id";
        
        _validatorMock.Setup(v => v.Validate(request)).Returns(true);
        _routerMock.Setup(r => r.SelectOptimalGatewayAsync(request)).ReturnsAsync(_gatewayMock.Object);
        _gatewayMock.Setup(g => g.ProcessPaymentAsync(request)).ReturnsAsync(true);
        _gatewayMock.Setup(g => g.GetCommissionAsync(Currency.USD)).ReturnsAsync(0.01m);

        // Запускаем несколько одновременных запросов с одним transactionId
        var tasks = new List<Task<Transaction>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_processor.ProcessPaymentAsync(request, transactionId));
        }

        var results = await Task.WhenAll(tasks);

        // Все результаты должны быть одинаковыми (идемпотентность)
        Assert.All(results, result => Assert.Equal(TransactionStatus.Processed, result.Status));
        Assert.All(results, result => Assert.Equal(transactionId, result.Id));
        
        // Gateway должен быть вызван только один раз, несмотря на 10 запросов
        _gatewayMock.Verify(g => g.ProcessPaymentAsync(request), Times.Once);
    }

    // Boundary cases: max amount, invalid inputs, etc.
}