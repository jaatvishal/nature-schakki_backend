using Asp.Versioning;
using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class PaymentsController(
    IPaymentService paymentService,
    IOrderService orderService,
    StoreContext context,
    IConfiguration config,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [Authorize]
    [HttpPost("create-intent/{orderId:int}")]
    public async Task<ActionResult<PaymentIntentDto>> CreatePaymentIntent(int orderId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var order = await context.Orders.FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);
        if (order == null) return NotFound();

        var paymentIntentId = await paymentService.CreatePaymentIntentAsync(order.Total);
        order.PaymentIntentId = paymentIntentId;
        await context.SaveChangesAsync();

        return Ok(new PaymentIntentDto(paymentIntentId, paymentIntentId));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = config["StripeSettings:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret) || webhookSecret.StartsWith("whsec_placeholder"))
        {
            logger.LogInformation("Stripe webhook received (mock mode).");
            return Ok();
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                    await ProcessSuccessfulPaymentAsync(paymentIntent.Id);
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                    await ProcessFailedPaymentAsync(paymentIntent.Id);
            }
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }

        return Ok();
    }

    private async Task ProcessSuccessfulPaymentAsync(string paymentIntentId)
    {
        if (await context.Payments.AnyAsync(p => p.PaymentIntentId == paymentIntentId && p.Status == PaymentStatus.Succeeded))
            return;

        var order = await context.Orders.FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId);
        if (order == null) return;

        if (order.Status == OrderStatus.Pending)
            await orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.PaymentReceived);

        var refreshed = await context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.Id);
        if (refreshed?.Status == OrderStatus.PaymentReceived)
            await orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);

        if (!await context.Payments.AnyAsync(p => p.PaymentIntentId == paymentIntentId))
        {
            context.Payments.Add(new Payment
            {
                OrderId = order.Id,
                PaymentIntentId = paymentIntentId,
                Amount = order.Total,
                Status = PaymentStatus.Succeeded
            });
            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessFailedPaymentAsync(string paymentIntentId)
    {
        if (await context.Payments.AnyAsync(p => p.PaymentIntentId == paymentIntentId && p.Status == PaymentStatus.Failed))
            return;

        var order = await context.Orders.FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId);
        if (order == null) return;

        if (order.Status == OrderStatus.Pending)
            await orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Failed);

        if (!await context.Payments.AnyAsync(p => p.PaymentIntentId == paymentIntentId))
        {
            context.Payments.Add(new Payment
            {
                OrderId = order.Id,
                PaymentIntentId = paymentIntentId,
                Amount = order.Total,
                Status = PaymentStatus.Failed
            });
            await context.SaveChangesAsync();
        }
    }
}
