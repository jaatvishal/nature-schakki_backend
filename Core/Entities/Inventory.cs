namespace Core.Entities;

public class Inventory : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
}
