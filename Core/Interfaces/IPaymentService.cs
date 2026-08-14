using Core.Entities;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd");
    Task<bool> RefundPaymentAsync(string paymentIntentId);
}
