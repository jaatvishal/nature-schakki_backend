namespace Core.DTOs;

public class AdminDashboardDto
{
    public int UserCount { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int CodOrders { get; set; }
    public decimal Revenue { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int LowStockCount { get; set; }
    public List<ChartPointDto> OrdersByStatus { get; set; } = [];
    public List<ChartPointDto> RevenueTrend { get; set; } = [];
    public List<TopProductDto> TopProducts { get; set; } = [];
    public List<AdminProductDto> LowStockProducts { get; set; } = [];
    public List<AdminOrderDto> RecentOrders { get; set; } = [];
    public List<AdminUserDto> RecentRegistrations { get; set; } = [];
    public List<AdminActivityDto> RecentActivities { get; set; } = [];
}

public class AdminActivityDto
{
    public required string Type { get; set; }
    public required string Description { get; set; }
    public DateTime OccurredAt { get; set; }
}
