using Core.Entities;

namespace Core.Specifications;

public class OrderWithItemsSpecification : BaseSpecification<Order>
{
    public OrderWithItemsSpecification(int orderId, int userId) : base(x => x.Id == orderId && x.UserId == userId)
    {
        AddInclude(x => x.OrderItems);
        AddInclude(x => x.DeliveryMethod!);
    }
}

public class OrdersForUserSpecification : BaseSpecification<Order>
{
    public OrdersForUserSpecification(int userId) : base(x => x.UserId == userId)
    {
        AddInclude(x => x.OrderItems);
        AddOrderDescending(x => x.OrderDate);
    }
}
