namespace Core.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
