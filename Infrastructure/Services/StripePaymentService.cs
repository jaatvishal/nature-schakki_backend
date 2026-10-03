using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Services;

public class StripePaymentService(IConfiguration config, ILogger<StripePaymentService> logger) : IPaymentService
{
    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
        var secretKey = config["StripeSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.StartsWith("sk_test_placeholder"))
        {
            logger.LogWarning("Stripe not configured; returning mock payment intent.");
            return $"pi_mock_{Guid.NewGuid():N}";
        }

        StripeConfiguration.ApiKey = secretKey;
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        });
        return intent.Id;
    }

    public async Task<bool> RefundPaymentAsync(string paymentIntentId)
    {
        if (paymentIntentId.StartsWith("pi_mock_"))
            return true;

        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
        var service = new RefundService();
        await service.CreateAsync(new RefundCreateOptions { PaymentIntent = paymentIntentId });
        return true;
    }
}
