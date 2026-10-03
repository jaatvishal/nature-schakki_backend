using Core.Enums;

namespace Core.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public required string PaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }
}
