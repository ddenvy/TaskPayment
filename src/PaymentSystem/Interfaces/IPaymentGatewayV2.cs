using System.Threading.Tasks;
using PaymentSystem.Models;

namespace PaymentSystem.Interfaces;

/// <summary>
/// интерфейс платёжного шлюза с поддержкой идемпотентности
/// </summary>
public interface IPaymentGatewayV2
{
    string Name { get; }
    
    /// <summary>
    /// Получить комиссию для валюты
    /// </summary>
    Task<decimal> GetCommissionAsync(Currency currency);
    
    /// <summary>
    /// Проверить доступность шлюза
    /// </summary>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// Проверить поддержку валюты
    /// </summary>
    bool SupportsCurrency(Currency currency);
    
    /// <summary>
    /// Обработать платёж с гарантией идемпотентности
    /// </summary>
    /// <param name="request">Запрос на платёж</param>
    /// <param name="transactionId">Уникальный идентификатор транзакции</param>
    /// <returns>Результат обработки платежа</returns>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, string transactionId);
    
    /// <summary>
    /// Получить статус платежа по идентификатору транзакции
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>Текущий статус платежа</returns>
    Task<PaymentResult> GetPaymentStatusAsync(string transactionId);
    
    /// <summary>
    /// Выполнить возврат средств с гарантией идемпотентности
    /// </summary>
    /// <param name="transactionId">Идентификатор оригинальной транзакции</param>
    /// <param name="amount">Сумма возврата</param>
    /// <param name="refundId">Уникальный идентификатор возврата</param>
    /// <returns>Результат возврата</returns>
    Task<RefundResult> RefundAsync(string transactionId, decimal amount, string refundId);
    
    /// <summary>
    /// Получить статус возврата по идентификатору
    /// </summary>
    /// <param name="refundId">Идентификатор возврата</param>
    /// <returns>Текущий статус возврата</returns>
    Task<RefundResult> GetRefundStatusAsync(string refundId);
    
    /// <summary>
    /// Отменить платёж (если поддерживается)
    /// </summary>
    /// <param name="transactionId">Идентификатор транзакции</param>
    /// <returns>Результат отмены</returns>
    Task<PaymentResult> CancelPaymentAsync(string transactionId);
}