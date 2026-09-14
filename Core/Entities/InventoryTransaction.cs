namespace Core.Entities;

public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int QuantityChange { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public required string Reason { get; set; }
    public int? ActorUserId { get; set; }
    public int? OrderId { get; set; }
}
