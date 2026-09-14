using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

public class SetUserStatusDto
{
    public bool IsActive { get; set; }
}

public class AdminProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string PictureUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
}

public class AdminProductUpsertDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Brand { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Category { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
    [Required, StringLength(30)]
    public string Unit { get; set; } = "kg";
    [Required, StringLength(500)]
    public string PictureUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class AdminCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class AdminCategoryUpsertDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdminOrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DeliveryCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<OrderItemDto> OrderItems { get; set; } = [];
    public IReadOnlyList<OrderStatusHistoryDto> Timeline { get; set; } = [];
}

public class OrderStatusHistoryDto
{
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class AdminInventoryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsLowStock { get; set; }
}

public class AdjustInventoryDto
{
    public int QuantityChange { get; set; }
    [Required, StringLength(200)]
    public string Reason { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    [Range(0, int.MaxValue)]
    public int? ReorderLevel { get; set; }
}

public class InventoryMovementDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityChange { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPaymentDto
{
    public int? Id { get; set; }
    public int OrderId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime Date { get; set; }
}

public class AdminReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public int CodOrders { get; set; }
    public int CancelledOrders { get; set; }
    public IReadOnlyList<ChartPointDto> SalesTrend { get; set; } = [];
    public IReadOnlyList<TopProductDto> TopProducts { get; set; } = [];
}

public class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class AdminAlertDto
{
    public string Severity { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
}

public class AdminAuditDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
