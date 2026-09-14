using Core.Enums;

namespace Core.Entities;

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public int? ChangedByUserId { get; set; }
}
