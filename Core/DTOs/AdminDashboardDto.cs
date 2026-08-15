namespace Core.DTOs;

public class AdminDashboardDto
{
    public int UserCount { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int LowStockCount { get; set; }
    public List<AdminActivityDto> RecentActivities { get; set; } = [];
}

public class AdminActivityDto
{
    public required string Type { get; set; }
    public required string Description { get; set; }
    public DateTime OccurredAt { get; set; }
}
