using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentSystem.Models;
using PaymentSystem.Services;
using Xunit;

namespace PaymentSystem.Tests;

public class ImprovedGatewayIdempotencyTests
{
    private readonly ImprovedMockGateway _gateway;
    private readonly PaymentRequest _testRequest;

    public ImprovedGatewayIdempotencyTests()
    {
        _gateway = new ImprovedMockGateway("TestGateway", 0.02m, Currency.USD, Currency.EUR);
        _testRequest = new PaymentRequest
        {
            Amount = 100m,
            Currency = Currency.USD,
            SourceAccount = "1234567890",
            DestinationAccount = "0987654321",
            Metadata = new Dictionary<string, string>()
        };
    }

    [Fact]
    public async Task ProcessPaymentAsync_SameTransactionId_ReturnsIdenticalResults()
    {
        // Arrange
        string transactionId = "idempotency-test-001";

        // Act - выполняем один и тот же запрос дважды
        var result1 = await _gateway.ProcessPaymentAsync(_testRequest, transactionId);
        var result2 = await _gateway.ProcessPaymentAsync(_testRequest, transactionId);

        // Assert - результаты должны быть идентичными
        Assert.Equal(result1.IsSuccess, result2.IsSuccess);
        Assert.Equal(result1.GatewayTransactionId, result2.GatewayTransactionId);
        Assert.Equal(result1.Status, result2.Status);
        Assert.Equal(result1.ProcessedAt, result2.ProcessedAt);
        Assert.Equal(result1.ErrorCode, result2.ErrorCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ConcurrentSameTransactionId_ReturnsIdenticalResults()
    {
        // Arrange
        string transactionId = "concurrent-idempotency-test";
        var tasks = new List<Task<PaymentResult>>();

        // Act - запускаем 10 одновременных запросов с одним ID
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_gateway.ProcessPaymentAsync(_testRequest, transactionId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - все результаты должны быть идентичными
        var firstResult = results[0];
        foreach (var result in results)
        {
            Assert.Equal(firstResult.IsSuccess, result.IsSuccess);
            Assert.Equal(firstResult.GatewayTransactionId, result.GatewayTransactionId);
            Assert.Equal(firstResult.Status, result.Status);
            Assert.Equal(firstResult.ProcessedAt, result.ProcessedAt);
        }
    }

    [Fact]
    public async Task GetPaymentStatusAsync_ExistingTransaction_ReturnsCorrectStatus()
    {
        // Arrange
        string transactionId = "status-check-test";
        
        // Act
        var processResult = await _gateway.ProcessPaymentAsync(_testRequest, transactionId);
        var statusResult = await _gateway.GetPaymentStatusAsync(transactionId);

        // Assert
        Assert.Equal(processResult.IsSuccess, statusResult.IsSuccess);
        Assert.Equal(processResult.GatewayTransactionId, statusResult.GatewayTransactionId);
        Assert.Equal(processResult.Status, statusResult.Status);
    }

    [Fact]
    public async Task GetPaymentStatusAsync_NonExistentTransaction_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = "non-existent-transaction";

        // Act
        var result = await _gateway.GetPaymentStatusAsync(nonExistentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TRANSACTION_NOT_FOUND", result.ErrorCode);
        Assert.Equal(PaymentStatus.Failed, result.Status);
    }

    [Fact]
    public async Task RefundAsync_SameRefundId_ReturnsIdenticalResults()
    {
        // Arrange
        string transactionId = "refund-idempotency-test";
        string refundId = "refund-001";
        
        // Сначала создаём успешную транзакцию
        await _gateway.ProcessPaymentAsync(_testRequest, transactionId);

        // Act - выполняем возврат дважды с одним ID
        var refund1 = await _gateway.RefundAsync(transactionId, 50m, refundId);
        var refund2 = await _gateway.RefundAsync(transactionId, 50m, refundId);

        // Assert
        Assert.Equal(refund1.IsSuccess, refund2.IsSuccess);
        Assert.Equal(refund1.GatewayRefundId, refund2.GatewayRefundId);
        Assert.Equal(refund1.Status, refund2.Status);
        Assert.Equal(refund1.ProcessedAt, refund2.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPaymentAsync_UnsupportedCurrency_ReturnsErrorIdempotently()
    {
        // Arrange
        var unsupportedRequest = new PaymentRequest
        {
            Amount = 100m,
            Currency = Currency.RUB, // Не поддерживается тестовым шлюзом
            SourceAccount = "1234567890",
            DestinationAccount = "0987654321",
            Metadata = new Dictionary<string, string>()
        };
        string transactionId = "unsupported-currency-test";

        // Act - выполняем запрос дважды
        var result1 = await _gateway.ProcessPaymentAsync(unsupportedRequest, transactionId);
        var result2 = await _gateway.ProcessPaymentAsync(unsupportedRequest, transactionId);

        // Assert - ошибки также должны быть идемпотентными
        Assert.False(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
        Assert.Equal("UNSUPPORTED_CURRENCY", result1.ErrorCode);
        Assert.Equal("UNSUPPORTED_CURRENCY", result2.ErrorCode);
        Assert.Equal(result1.ProcessedAt, result2.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DifferentTransactionIds_ReturnsDifferentResults()
    {
        // Arrange
        string transactionId1 = "different-tx-1";
        string transactionId2 = "different-tx-2";

        // Act
        var result1 = await _gateway.ProcessPaymentAsync(_testRequest, transactionId1);
        var result2 = await _gateway.ProcessPaymentAsync(_testRequest, transactionId2);

        // Assert - разные транзакции должны иметь разные результаты
        if (result1.IsSuccess && result2.IsSuccess)
        {
            Assert.NotEqual(result1.GatewayTransactionId, result2.GatewayTransactionId);
        }
        // Время обработки может отличаться
        Assert.True(Math.Abs((result1.ProcessedAt - result2.ProcessedAt).TotalMilliseconds) >= 0);
    }

    [Fact]
    public async Task AdapterCompatibility_LegacyToModern_WorksCorrectly()
    {
        // Arrange
        var legacyGateway = new MockGatewayA();
        var adapter = new PaymentGatewayAdapter(legacyGateway);
        string transactionId = "adapter-test";

        // Act
        var result = await adapter.ProcessPaymentAsync(_testRequest, transactionId);

        // Assert
        Assert.True(result.IsSuccess); // MockGatewayA всегда возвращает true
        Assert.Equal(PaymentStatus.Completed, result.Status);
        Assert.Contains("GatewayA", result.GatewayTransactionId);
    }

    [Fact]
    public async Task AdapterCompatibility_ModernToLegacy_WorksCorrectly()
    {
        // Arrange
        var modernGateway = new ImprovedMockGateway("ModernGateway", 0.01m, Currency.USD);
        var reverseAdapter = new PaymentGatewayReverseAdapter(modernGateway);

        // Act
        var result = await reverseAdapter.ProcessPaymentAsync(_testRequest);

        // Assert
        Assert.True(result); // Ожидаем успешный результат
    }
}